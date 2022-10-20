using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;
using Level = Autodesk.Revit.DB.Level;
using Line = Autodesk.Revit.DB.Line;
using Parameter = Autodesk.Revit.DB.Parameter;
using UnitType = Autodesk.Revit.DB.UnitType;


namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutVoidCollisionManager : IDisposable
    {

        #region Default Properties

        private readonly XYZ basisZNormal = XYZ.BasisZ;
        private Units revitUnits { get; set; } = null;
        private DisplayUnitType angleUnit { get; set; }
        private Options options { get; } = new()
        {
            ComputeReferences = true,
            IncludeNonVisibleObjects = true,
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

        private ElementTypeData sizeData;
        private FilteredElementCollector collector;
        private SketchPlane levelSketch;
        private Plane levelPlane;
        #endregion


        #region Templory Properties

        private Line line = null;
        private ElementId instanceId = null;
        private XYZ offset = null;
        private XYZ centroid = null;
        private Solid hostSolid = null;
        private Solid interSolid = null;
        private XYZ interNormal = XYZ.BasisZ;
        private XYZ hostNormal = XYZ.BasisZ;
        private BoundingBoxXYZ interBbox = null;
        private BoundingBoxXYZ hostBbox = null;
        private Transform transform = Transform.Identity;

        private string unique = string.Empty;

        private double angleRadians = 0;
        private double angleHorisontDegrees = 0;
        private double angleVerticalDegrees = 0;
        private double diameter = 0;
        private double hight = 0;
        private double widht = 0;
        private int count = 0;
        private TransactionStatus status;

        #endregion


        #region Cache

        private readonly ICollection<ElementId> idsExclude = new List<ElementId>();
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


        private void CreateSketchPlaneByLevel(Document doc, Level level)
        {
            using Transaction transaction = new(doc, "CreateSketchPlane");
            if (transaction.Start() == TransactionStatus.Started)
            {
                try
                {
                    levelPlane = Plane.CreateByNormalAndOrigin(basisZNormal, new XYZ(0, 0, level.Elevation));
                    levelSketch = SketchPlane.Create(doc, levelPlane);
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
        }


        public IList<ElementModel> GetCollisionByLevel(Document doc, Level level, IEnumerable<Element> elements)
        {
            count = 0;
            InitializeCache(doc);
            levelIntId = level.Id.IntegerValue;
            CreateSketchPlaneByLevel(doc, level);
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
            hostSolid = host.GetSolidByVolume(identityTransform, options);
            hostNormal = host is Wall wall ? wall.Orientation : basisZNormal;
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBbox.GetOutLine());
            LogicalAndFilter intersectFilter = new(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).WherePasses(intersectFilter).OfCategoryId(catId);
            foreach (Element elem in collector)
            {
                centroid = elem.GetMiddlePointByBoundingBox(ref interBbox);
                if (IsValidIntersection(elem, hostSolid, centroid, minSideSize, out interNormal))
                {
                    if (IsNotParallel(hostNormal, interNormal))
                    {
                        count++;
                        interSolid = elem.GetIntersectionSolid(global, hostSolid, options);
                        interBbox = interSolid?.GetBoundingBox();
                        if (interBbox != null)
                        {
                            instanceId = elem.Id;

                            idsExclude.Add(instanceId);

                            XYZ min = interBbox.Min;
                            XYZ max = interBbox.Max;

                            XYZ minX = new XYZ(max.X, min.Y, min.Z);
                            XYZ minY = new XYZ(min.X, max.Y, min.Z);
                            XYZ minZ = new XYZ(min.X, min.Y, max.Z);

                            XYZ maxX = new XYZ(min.X, max.Y, max.Z);
                            XYZ maxY = new XYZ(max.X, min.Y, max.Z);
                            XYZ maxZ = new XYZ(max.X, max.Y, min.Z);

                            angleRadians = XYZ.BasisX.AngleOnPlaneTo(hostNormal, basisZNormal);
                            Plane section = Plane.CreateByNormalAndOrigin(hostNormal, centroid);
                            Transform vertical = Transform.CreateRotationAtPoint(XYZ.BasisZ, angleRadians, centroid);
                            Transform horizont = Transform.CreateRotationAtPoint(XYZ.BasisX, angleRadians, centroid);
                            Transform transform = vertical.Multiply(horizont);

                            min = transform.OfPoint(ProjectOnto(section, min));
                            max = transform.OfPoint(ProjectOnto(section, max));
                            minX = transform.OfPoint(ProjectOnto(section, minX));
                            minY = transform.OfPoint(ProjectOnto(section, minY));
                            minZ = transform.OfPoint(ProjectOnto(section, minZ));
                            maxX = transform.OfPoint(ProjectOnto(section, maxX));
                            maxY = transform.OfPoint(ProjectOnto(section, maxY));
                            maxZ = transform.OfPoint(ProjectOnto(section, maxZ));


                            //var angleX = hostNormal.AngleTo(XYZ.BasisX);
                            //var angleY = hostNormal.AngleTo(XYZ.BasisY);
                            //var angleZ = hostNormal.AngleTo(XYZ.BasisZ);

                            

                            




                            //_ = Math.Abs(max.Z - min.Z);

                            _ = interSolid.GetCountours(doc, levelPlane, levelSketch, cutOffsetSize);


                            //_ = GeometryCreationUtilities.CreateExtrusionGeometry(curveloops, basisZNormal, height);

                            sizeData = GetSectionSize(elem, interSolid, interNormal);

                            ElementModel model = new(instanceId, sizeData)
                            {
                                Origin = centroid,
                                HostNormal = hostNormal,
                                ModelNormal = interNormal,
                                LevelIntId = levelIntId,
                                HostIntId = host.Id.IntegerValue,
                                SymbolName = sizeData.SymbolName,
                                FamilyName = sizeData.FamilyName,
                                Description = GetSizeDescription(sizeData)
                            };

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
        //    Outline outline = new(bbox.Min -= offset, bbox.Max += offset);
        //    collector = new FilteredElementCollector(doc, view.Id).Excluding(idsExclude);
        //    instanceId = collector.WherePasses(new BoundingBoxIntersectsFilter(outline)).FirstElementId();
        //    return instanceId == null;
        //}


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



        private ElementTypeData GetSectionSize(Element elem, Solid solid, XYZ direction)
        {
            ElementTypeData structData = new(null);
            int catIdInt = elem.Category.Id.IntegerValue;
            if (elem.Document.GetElement(elem.GetTypeId()) is ElementType etype)
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
                                structData = GetSizeByGeometry(solid, etype, direction);
                                sizeTempData.Add(unique, structData);
                            }
                            return structData;
                        }
                }
            }
            return structData;
        }


        private ElementTypeData GetSizeByGeometry(Solid solid, ElementType etype, XYZ direction)
        {
            ElementTypeData result = new(null);
            centroid = solid.ComputeCentroid();
            direction = ResetDirectionToPositive(direction);
            angleHorisontDegrees = ConvertRadiansToDegrees(GetHorizontAngleRadiansByNormal(direction));
            angleVerticalDegrees = ConvertRadiansToDegrees(GetVerticalAngleRadiansByNormal(direction));
            Transform horizont = Transform.CreateRotationAtPoint(identityTransform.BasisZ, GetInternalAngle(angleHorisontDegrees), centroid);
            Transform vertical = Transform.CreateRotationAtPoint(identityTransform.BasisX, GetInternalAngle(angleVerticalDegrees), centroid);
            solid = angleHorisontDegrees == 0 ? solid : SolidUtils.CreateTransformed(solid, horizont);
            solid = angleVerticalDegrees == 0 ? solid : SolidUtils.CreateTransformed(solid, vertical);
            interBbox = solid?.GetBoundingBox();
            if (interBbox != null)
            {
                widht = Math.Abs(interBbox.Max.X - interBbox.Min.X);
                hight = Math.Abs(interBbox.Max.Z - interBbox.Min.Z);
                result = new ElementTypeData(etype, hight, widht);
            }
            return result;
        }


        private string GetSizeDescription(ElementTypeData typeData)
        {
            if (typeData.IsValidObject)
            {
                int h = (int)Math.Round(typeData.Height * 304.8);
                int w = (int)Math.Round(typeData.Width * 304.8);
                return $"{w}x{h}(h)".Normalize();
            }
            return string.Empty;
        }


        private bool IsNotParallel(XYZ hostNormal, XYZ direction)
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


        //private void CreateDirectShape(Document doc, Instance elem, Centroid solid)
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


        //private static IEnumerable<CurveLoop> GetCountours(Centroid solid, Instance elem)
        //{
        //    try
        //    {
        //        Plane section = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, elem.get_BoundingBox(null).Min);
        //        ExtrusionAnalyzer analyzer = ExtrusionAnalyzer.Create(solid, section, XYZ.BasisZ);
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
