using Autodesk.Revit.DB;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;
using Parameter = Autodesk.Revit.DB.Parameter;
using UnitType = Autodesk.Revit.DB.UnitType;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutVoidCollisionManager : IDisposable
    {
        #region Default Properties
        private Units revitUnits { get; set; } = null;
        private DisplayUnitType angleUnit { get; set; }
        private Options options { get; } = new()
        {
            ComputeReferences = true,
            IncludeNonVisibleObjects = false,
            DetailLevel = ViewDetailLevel.Medium
        };

        private Transform identityTransform { get; } = Transform.Identity;
        private SolidCurveIntersectionOptions intersectOptions { get; } = new();

        #endregion


        #region Constant Properties

        private const int invalidInt = -1;
        private const double footToMm = 304.8;
        private const double rightAngle = Math.PI / 2;
        private readonly double thresholdAngle = Math.Floor(Math.Cos(45 * Math.PI / 180));

        #endregion


        #region Input Properties

        private double minDistance { get; set; } = 0;
        public int LevelIntId { get; set; } = invalidInt;
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


        #region Templory Properties

        private Line line = null;
        private XYZ offset = null;
        private XYZ centroid = null;
        private Solid hostSolid = null;
        private Solid interSolid = null;
        private ElementType instType = null;
        private XYZ instNormal = XYZ.BasisZ;
        private XYZ hostNormal = XYZ.BasisZ;
        private string unique = string.Empty;
        private BoundingBoxXYZ instBbox = null;
        private BoundingBoxXYZ hostBbox = null;
        private Transform transform = Transform.Identity;

        private FilteredElementCollector collector = null;

        private double angleRadians = 0;
        private double angleHorisontDegrees = 0;
        private double angleVerticalDegrees = 0;
        private double diameter = 0;
        private double hight = 0;
        private double widht = 0;

        #endregion


        #region Cache

        private ICollection<ElementId> idsExclude = new List<ElementId>();
        private ElementModel[] modelTempData { get; set; } = new ElementModel[0];
        private IDictionary<string, ElementTypeData> sizeTempData { get; set; } = CacheDataRepository.SizeTypeData;

        #endregion


        private void InitializeCache(Document doc)
        {
            revitUnits = doc.GetUnits();
            modelTempData = new ElementModel[100];
            offset = new XYZ(cutOffsetSize, cutOffsetSize, cutOffsetSize);
            minDistance = cutOffsetSize + ((minSideSize + maxSideSize) * 0.25);
            angleUnit = revitUnits.GetFormatOptions(UnitType.UT_Angle).DisplayUnits;
        }


        public IEnumerable<ElementModel> GetCollisionByLevel(Document doc, Level level, IEnumerable<Element> elements)
        {
            InitializeCache(doc);
            LevelIntId = level.Id.IntegerValue;
            foreach (Element host in elements)
            {
                if (LevelIntId == host.LevelId.IntegerValue)
                {
                    foreach (ElementModel model in GetIntersectionModelByHost(doc, host, SearchGlobal, SearchCatId))
                    {
                        yield return model;
                    }
                }
            }
        }


        private IEnumerable<ElementModel> GetIntersectionModelByHost(Document doc, Element host, Transform global, ElementId catId)
        {
            int hostIdInt = host.Id.IntegerValue;
            hostBbox = host.get_BoundingBox(null);
            hostSolid = host.GetSolidByVolume(identityTransform, options);
            hostNormal = host is Wall wall ? wall.Orientation : XYZ.BasisZ;
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBbox.GetOutLine());
            LogicalAndFilter intersectFilter = new(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).WherePasses(intersectFilter).OfCategoryId(catId);
            foreach (Element instance in collector)
            {
                centroid = instance.GetMiddlePointByBoundingBox(ref instBbox);
                if (IsValidIntersection(instance, hostSolid, centroid, minSideSize, out instNormal))
                {
                    if (IsNotParallelDirections(hostNormal, instNormal))
                    {
                        interSolid = instance.GetIntersectionSolid(global, hostSolid, options);
                        if (interSolid != null)
                        {
                            centroid = interSolid.ComputeCentroid();
                            instType = instance.Document.GetElement(instance.GetTypeId()) as ElementType;
                            ElementTypeData sizeData = GetSectionSize(instance, instType, instNormal);
                            yield return new ElementModel(instance, sizeData, hostIdInt, instBbox);
                        }
                    }
                }
            }
        }


        private bool CreateOpening(ElementModel model)
        {
            return true;
        }


        private bool CheckSizeOpenning(BoundingBoxXYZ bbox)
        {
            Outline outline = new(bbox.Min -= offset, bbox.Max += offset);
            BoundingBoxIntersectsFilter bbfilter = new BoundingBoxIntersectsFilter(outline);
            return true;
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



        private ElementTypeData GetSectionSize(Element elem, ElementType etype, XYZ direction)
        {
            ElementTypeData structData = new(null);
            int catIdInt = elem.Category.Id.IntegerValue;
            if (Enum.IsDefined(typeof(BuiltInCategory), catIdInt))
            {
                BuiltInCategory builtInCategory = (BuiltInCategory)catIdInt;
                switch (builtInCategory)
                {
                    case BuiltInCategory.OST_PipeCurves:
                        {
                            diameter = elem.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                            return new ElementTypeData(etype, diameter, diameter);
                        }
                    case BuiltInCategory.OST_DuctCurves:
                        {
                            Parameter diameterParam = elem.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                            if (diameterParam != null && diameterParam.HasValue)
                            {
                                diameter = diameterParam.AsDouble();
                                return new ElementTypeData(etype, diameter, diameter);
                            }
                            else
                            {
                                hight = elem.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
                                widht = elem.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
                                return new ElementTypeData(etype, hight, widht);
                            }
                        }
                    case BuiltInCategory.OST_Conduit:
                        {
                            diameter = elem.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).AsDouble();
                            return new ElementTypeData(etype, diameter, diameter);
                        }
                    case BuiltInCategory.OST_CableTray:
                        {
                            hight = elem.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();
                            widht = elem.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
                            return new ElementTypeData(etype, hight, widht);
                        }
                    default:
                        {
                            unique = etype.UniqueId.Normalize();
                            if (!sizeTempData.TryGetValue(unique, out structData))
                            {
                                structData = GetSizeByGeometry(etype, direction);
                                sizeTempData.Add(unique, structData);
                            }
                            return structData;
                        }
                }
            }
            return structData;
        }


        private ElementTypeData GetSizeByGeometry(ElementType etype, XYZ direction)
        {
            transform = identityTransform;
            ElementTypeData result = new(null);
            direction = ResetDirectionToPositive(direction);
            angleHorisontDegrees = ConvertRadiansToDegrees(GetHorizontAngleRadiansByNormal(direction));
            angleVerticalDegrees = ConvertRadiansToDegrees(GetVerticalAngleRadiansByNormal(direction));
            Transform horizont = Transform.CreateRotationAtPoint(identityTransform.BasisZ, GetInternalAngle(angleHorisontDegrees), centroid);
            Transform vertical = Transform.CreateRotationAtPoint(identityTransform.BasisX, GetInternalAngle(angleVerticalDegrees), centroid);
            interSolid = angleHorisontDegrees == 0 ? interSolid : SolidUtils.CreateTransformed(interSolid, horizont);
            interSolid = angleVerticalDegrees == 0 ? interSolid : SolidUtils.CreateTransformed(interSolid, vertical);
            instBbox = interSolid?.GetBoundingBox();
            if (instBbox != null)
            {
                widht = Math.Abs(instBbox.Max.X - instBbox.Min.X);
                hight = Math.Abs(instBbox.Max.Z - instBbox.Min.Z);
                result = new ElementTypeData(etype, hight, widht);
            }
            return result;
        }


        private bool IsNotParallelDirections(XYZ hostNormal, XYZ direction)
        {
            return !direction.IsAlmostEqualTo(XYZ.Zero) && Math.Abs(hostNormal.DotProduct(direction)) > thresholdAngle;
        }


        private XYZ ResetDirectionToPositive(XYZ direction)
        {
            angleRadians = XYZ.BasisX.AngleOnPlaneTo(direction, XYZ.BasisZ);
            return angleRadians < Math.PI ? direction : direction.Negate();
        }


        private double GetHorizontAngleRadiansByNormal(XYZ direction)
        {
            return Math.Atan(direction.X / direction.Y);
        }


        private double GetVerticalAngleRadiansByNormal(XYZ direction)
        {
            return Math.Acos(direction.DotProduct(XYZ.BasisZ)) - rightAngle;
        }


        private double GetInternalAngle(double degrees)
        {
            return UnitUtils.ConvertToInternalUnits(degrees, angleUnit);
        }


        private double ConvertRadiansToDegrees(double radians, int digit = 5)
        {
            return Math.Round(180 / Math.PI * radians, digit);
        }


        #region Other methods

        //private double GetLengthValueBySimilarParameterName(Element elem, string paramName)
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


        //private void CreateDirectShape(Document doc, Element elem, Centroid solid)
        //{
        //    using Transaction trans = new(doc, "Create DirectShape");
        //    try
        //    {
        //        _ = trans.Start();
        //        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
        //        ds.ApplicationDataId = elem.UniqueId;
        //        ds.Name = "Centroid by " + elem.Name;
        //        ds.SetShape(new GeometryObject[] { solid });
        //        _ = trans.Commit();
        //    }
        //    catch (Exception exc)
        //    {
        //        _ = trans.RollBack();
        //        Logger.Error(exc.Message);
        //    }
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
        //            SketchPlane sketch = SketchPlane.Create(doc, reference);
        //            if (null != sketch)
        //            {
        //                Plane plan = sketch.GetPlane();
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


        //private double GetRotationAngleFromTransform(Transform global)
        //{
        //    double x = global.BasisX.X;
        //    double y = global.BasisY.Y;
        //    double z = global.BasisZ.Z;
        //    double trace = x + y + z;
        //    return Math.Acos((trace - 1) / 2.0);
        //}


        //private static IEnumerable<CurveLoop> GetCountours(Centroid solid, Element elem)
        //{
        //    try
        //    {
        //        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, elem.get_BoundingBox(null).Min);
        //        ExtrusionAnalyzer analyzer = ExtrusionAnalyzer.Create(solid, plane, XYZ.BasisZ);
        //        Face face = analyzer.GetExtrusionBase();
        //        return face.GetEdgesAsCurveLoops();
        //    }
        //    catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        //    {
        //        return Enumerable.Empty<CurveLoop>();
        //    }
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
            sizeTempData.Clear();
            transform?.Dispose();
            SearchDoc?.Dispose();
            interSolid?.Dispose();
            SearchGlobal?.Dispose();
            SearchInstance?.Dispose();

            Logger.Log(sizeTempData.Values.Count.ToString());
        }
    }
}
