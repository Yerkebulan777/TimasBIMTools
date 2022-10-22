﻿using Autodesk.Revit.DB;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;
using Level = Autodesk.Revit.DB.Level;
using Line = Autodesk.Revit.DB.Line;
using Parameter = Autodesk.Revit.DB.Parameter;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutVoidCollisionManager : IDisposable
    {

        #region Default Properties

        private readonly Options options = new()
        {
            ComputeReferences = true,
            IncludeNonVisibleObjects = true,
            DetailLevel = ViewDetailLevel.Medium
        };

        private readonly XYZ basisZNormal = XYZ.BasisZ;
        private readonly Transform identity = Transform.Identity;
        private readonly XYZ vertExis = Transform.Identity.BasisX;
        private readonly XYZ horzExis = Transform.Identity.BasisZ;
        private readonly SolidCurveIntersectionOptions intersectOptions = new();

        #endregion


        #region Constant Properties

        private const int invalidInt = -1;
        private const double footToMm = 304.8;
        private readonly double thresholdAngle = Math.Floor(Math.Cos(45 * Math.PI / 180));

        #endregion


        #region Input Properties

        private double minDistance { get; set; } = 0;
        public int levelIntId { get; set; } = invalidInt;
        public Document SearchDoc { get; internal set; } = null;
        public ElementId SearchCatId { get; internal set; } = null;
        public Transform SearchGlobal { get; internal set; } = null;
        public RevitLinkInstance SearchInstance { get; internal set; } = null;

        private readonly double minSideSize = Convert.ToDouble(Properties.Settings.Default.MinSideSizeInMm / footToMm);
        private readonly double maxSideSize = Convert.ToDouble(Properties.Settings.Default.MaxSideSizeInMm / footToMm);
        private readonly double cutOffsetSize = Convert.ToDouble(Properties.Settings.Default.CutOffsetInMm / footToMm);

        //private readonly string widthParamName = "ширина";
        //private readonly string heightParamName = "высота";

        #endregion


        #region Output Properties

        private FilteredElementCollector collector;
        private SketchPlane sketchPlan;
        private Plane plane;
        private double diameter = 0;
        private double hight = 0;
        private double widht = 0;
        private int count = 0;
        private TransactionStatus status;

        #endregion


        #region Templory Properties

        private Line line = null;
        private XYZ offsetPnt = null;
        private XYZ centroid = null;
        private Solid hostSolid = null;
        private Solid interSolid = null;
        private XYZ hostNormal = XYZ.BasisZ;
        private XYZ interNormal = XYZ.BasisZ;
        private Transform transform = null;
        private BoundingBoxXYZ hostBbox = null;
        private BoundingBoxXYZ interBbox = null;
        private readonly ElementId instanceId = null;

        #endregion


        #region Cache Properties

        private ElementModel[] modelTempData = new ElementModel[0];
        private readonly ICollection<ElementId> idsExclude = new List<ElementId>(50);

        #endregion


        private void InitializeCache(Document doc)
        {
            modelTempData = new ElementModel[100];
            offsetPnt = new XYZ(cutOffsetSize, cutOffsetSize, cutOffsetSize);
            minDistance = cutOffsetSize + ((minSideSize + maxSideSize) * 0.25);
        }


        private SketchPlane CreateSketchPlaneByNormal(Document doc, XYZ normal, XYZ point)
        {
            SketchPlane result = null;
            using Transaction transaction = new(doc, "CreateSketchPlane");
            if (transaction.Start() == TransactionStatus.Started)
            {
                try
                {
                    plane = Plane.CreateByNormalAndOrigin(normal, point);
                    result = SketchPlane.Create(doc, plane);
                    status = transaction.Commit();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    if (!transaction.HasEnded())
                    {
                        status = transaction.RollBack();
                    }
                }
            }
            return result;
        }


        public IList<ElementModel> GetCollisionByLevel(Document doc, Level level, IEnumerable<Element> elements)
        {
            count = 0;
            InitializeCache(doc);
            levelIntId = level.Id.IntegerValue;
            IList<ElementModel> output = new List<ElementModel>(50);
            using TransactionGroup transGroup = new(doc, "GetCollision");
            status = transGroup.Start();
            foreach (Element host in elements)
            {
                if (levelIntId == host.LevelId.IntegerValue)
                {
                    foreach (ElementModel model in GetIntersectionByElement(doc, host, SearchGlobal, SearchCatId))
                    {
                        output.Add(model);
                    }
                }
            }
            status = transGroup.Assimilate();
            Logger.Info("Result count: " + count);
            return output;
        }


        private IEnumerable<ElementModel> GetIntersectionByElement(Document doc, Element host, Transform global, ElementId catId)
        {
            hostBbox = host.get_BoundingBox(null);
            hostSolid = host.GetSolidByVolume(identity, options);
            hostNormal = host is Wall wall ? wall.Orientation.Normalize() : host.GetNormalByTopFace(identity);
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBbox.GetOutLine());
            LogicalAndFilter intersectFilter = new(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).WherePasses(intersectFilter).OfCategoryId(catId);
            foreach (Element elem in collector)
            {
                centroid = elem.GetMiddlePointByBoundingBox(ref interBbox);
                if (IsValidIntersection(elem, hostSolid, centroid, minSideSize, out interNormal))
                {
                    if (IsValidParallel(hostNormal, interNormal))
                    {
                        interSolid = hostSolid.GetIntersectionSolid(elem, global, options);
                        if (interSolid != null)
                        {
                            count++;
                            interBbox = interSolid.GetBoundingBox();
                            centroid = interSolid.ComputeCentroid();
                            interNormal = interNormal.ResetDirectionToPositive();
                            sketchPlan = CreateSketchPlaneByNormal(doc, interNormal, centroid);
                            Tuple<double, double> tupleSize = interSolid.GetCountours(doc, plane, sketchPlan, cutOffsetSize);

                            ElementModel model = new(elem)
                            {
                                Origin = centroid,
                                LevelIntId = levelIntId,
                                HostNormal = hostNormal,
                                ModelNormal = interNormal,
                                HostIntId = host.Id.IntegerValue,
                            };
                            model.SetSizeDescription(tupleSize.Item1, tupleSize.Item2);

                            yield return model;
                        }
                    }
                }
            }
        }


        private XYZ ProjectOnto(Plane plane, XYZ pnt)
        {
            double distance = SignedDistanceTo(plane, pnt);
            XYZ result = pnt - (distance * plane.Normal);
            return result;
        }


        private double SignedDistanceTo(Plane plane, XYZ pnt)
        {
            XYZ vector = pnt - plane.Origin;
            return plane.Normal.DotProduct(vector);
        }


        //private Outline CreateOpening(Document doc, ElementModel model)
        //{


        //    return null;
        //}


        //private bool ComputeIntersectionVolume(Solid solidA, Solid solidB)
        //{
        //    Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(solidA, solidB, BooleanOperationsType.Intersect);
        //    return intersection.Volume > 0;
        //}


        //private bool CheckSizeOpenning(Document doc, BoundingBoxXYZ bbox, XYZ normal, View view)
        //{
        //    Outline outline = new(bbox.Min -= offsetPnt, bbox.Max += offsetPnt);
        //    collector = new FilteredElementCollector(doc, view.Id).Excluding(idsExclude);
        //    instanceId = collector.WherePasses(new BoundingBoxIntersectsFilter(outline)).FirstElementId();
        //    return instanceId == null;
        //}


        private bool IsValidParallel(XYZ hostNormal, XYZ direction)
        {
            return !direction.IsAlmostEqualTo(XYZ.Zero) && Math.Abs(hostNormal.DotProduct(direction)) > thresholdAngle;
        }


        private bool IsValidIntersection(Element elem, Solid solid, XYZ centroid, double tolerance, out XYZ normal)
        {
            double length = 0;
            normal = XYZ.Zero;
            if (elem is FamilyInstance instance)
            {
                transform = instance.GetTransform();
                normal = transform.BasisX.Normalize();
                XYZ strPnt = centroid - (normal * maxSideSize);
                XYZ endPnt = centroid + (normal * maxSideSize);
                line = Line.CreateBound(strPnt, endPnt);
            }
            else if (elem.Location is LocationCurve curve)
            {
                line = curve.Curve as Line;
                normal = line.Direction.Normalize();
            }
            SolidCurveIntersection curves = solid.IntersectWithCurve(line, intersectOptions);
            if (curves != null && 0 < curves.SegmentCount)
            {
                length = curves.GetCurveSegment(0).Length;
            }
            return length > tolerance;
        }


        private void GetSectionSize(Element elem)
        {
            hight = 0; widht = 0;
            int catIdInt = elem.Category.Id.IntegerValue;
            if (elem.Document.GetElement(elem.GetTypeId()) is ElementType)
            {
                BuiltInCategory builtInCategory = (BuiltInCategory)catIdInt;
                switch (builtInCategory)
                {
                    case BuiltInCategory.OST_PipeCurves:
                        {
                            diameter = elem.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                            return;
                        }
                    case BuiltInCategory.OST_DuctCurves:
                        {
                            Parameter diameterParam = elem.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                            if (diameterParam != null && diameterParam.HasValue)
                            {
                                diameter = diameterParam.AsDouble();
                                hight = diameter;
                                widht = diameter;
                            }
                            else
                            {
                                hight = elem.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
                                widht = elem.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
                            }
                            return;
                        }
                    case BuiltInCategory.OST_Conduit:
                        {
                            diameter = elem.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).AsDouble();
                            return;
                        }
                    case BuiltInCategory.OST_CableTray:
                        {
                            hight = elem.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();
                            widht = elem.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
                            return;
                        }
                    default:
                        {
                            return;
                        }
                }
            }
        }


        #region Other methods

        //private double GetLengthValueBySimilarParameterName(Instance elem, string paramName)
        //{
        //    double value = invalidInt;
        //    int minDistance = int.MaxValue;
        //    char[] delimiters = new[] { ' ', '_', '-' };
        //    foreach (Parameter param in elem.GetOrderedParameters())
        //    {
        //        Definition definition = param.Definition;
        //        if (param.HasValue && definition.ParameterType == lenParamType)
        //        {
        //            string name = definition.Name;
        //            string[] strArray = name.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        //            if (strArray.Contains(paramName, StringComparer.CurrentCultureIgnoreCase))
        //            {
        //                int tmp = param.IsShared ? name.Length : name.Length + strArray.Length;
        //                if (minDistance > tmp && UnitFormatUtils.TryParse(revitUnits, UnitType.UT_Length, param.AsValueString(), out value))
        //                {
        //                    minDistance = tmp;
        //                }
        //            }
        //        }
        //    }
        //    return value;
        //}


        //private bool GetFamilyInstanceReferencePlane(FamilyInstance fi, out XYZ origin, out XYZ normal)
        //{
        //    bool flag = false;
        //    origin = XYZ.Zero;
        //    normal = XYZ.Zero;

        //    Reference reference = fi.GetReferences(FamilyInstanceReferenceType.CenterFrontBack).FirstOrDefault();
        //    reference = SearchInstance != null ? reference.CreateLinkReference(SearchInstance) : reference;

        //    if (null != reference)
        //    {
        //        Document doc = fi.Document;
        //        using Transaction transaction = new(doc);
        //        _ = transaction.Start("Create Temporary Sketch Plane");
        //        try
        //        {
        //            SketchPlane sketchPlan = SketchPlane.Create(doc, reference);
        //            if (null != sketchPlan)
        //            {
        //                Plane plan = sketchPlan.GetPlane();
        //                normal = plan.Normal;
        //                origin = plan.Origin;
        //                flag = true;
        //            }
        //        }
        //        finally
        //        {
        //            _ = transaction.RollBack();
        //        }
        //    }
        //    return flag;
        //}


        //private double GetRotationAngleFromTransform(Transform local)
        //{
        //    double x = local.BasisX.X;
        //    double y = local.BasisY.Y;
        //    double z = local.BasisZ.Z;
        //    double trace = x + y + z;
        //    return Math.Acos((trace - 1) / 2.0);
        //}

        #endregion


        /// Алгоритм проверки семейств отверстия
        /*
        * Проверить семейство что это реальное отверстие
        * Найти все семейства и определить пересекается ли оно с чем либо (по краю)
        * Если не пересекается проверить есть ли по центру елемент если нет то удалить
        * Если пересекается то удалить
        */


        /// Общий алгоритм проверки пользователем елементов
        /*
         * Объединения елементов в одной точке в один большой bbox если они пересекаются
         * Объединения проема если пересекаются bbox или находятся очень близко
         * Создать новое семейство проема с возможностью изменения размеров => CutOffset сохраняется
         * Реализовать автосинхронизацию при окончание выполнение или изменения проекта
         * Кнопки = (показать/создать/остановить)
         * Необходимо использовать Dispose()
         */


        [STAThread]
        public void Dispose()
        {
            transform?.Dispose();
            SearchDoc?.Dispose();
            hostSolid?.Dispose();
            interSolid?.Dispose();
            SearchGlobal?.Dispose();
            SearchInstance?.Dispose();
        }
    }
}
