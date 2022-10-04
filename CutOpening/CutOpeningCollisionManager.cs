using Autodesk.Revit.DB;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;

namespace RevitTimasBIMTools.CutOpening
{
    internal sealed class CutOpeningCollisionManager : IDisposable
    {
        #region Parameter Properties

        private Units units = null;
        private DisplayUnitType angleUnit;
        private readonly Options options = new()
        {
            ComputeReferences = true,
            IncludeNonVisibleObjects = false,
            DetailLevel = ViewDetailLevel.Medium
        };

        private readonly Transform identityTransform = Transform.Identity;
        #endregion


        #region Constant Properties

        private const int invalIdInt = -1;
        private const double toleranceVolume = 0.005;
        private const double rightAngle = Math.PI / 2;
        private readonly double thresholdAngle = Math.Round(Math.Cos(45 * Math.PI / 180), 5);

        #endregion


        #region Input Properties

        private int levelIntId = 0;
        private Document searchDoc = null;
        private Transform searchTrans = null;
        private ElementId searchCatId = null;
        private RevitLinkInstance searchInstance = null;

        private readonly int minSideSize = Properties.Settings.Default.MinSideSizeInMm;
        private readonly int maxSideSize = Properties.Settings.Default.MaxSideSizeInMm;
        private readonly int cutOffsetSize = Properties.Settings.Default.CutOffsetInMm;

        //private readonly string widthParamName = "ширина";
        //private readonly string heightParamName = "высота";

        #endregion


        #region Templory Properties


        private ElementDataDictionary dataBase;
        private XYZ centroidPoint = XYZ.Zero;
        private XYZ intersectNormal = XYZ.BasisZ;
        private XYZ hostDirection = XYZ.BasisZ;

        private Solid hostSolid = null;
        private Solid intersectSolid = null;
        private string unique = string.Empty;
        private Transform transform = Transform.Identity;
        private FilteredElementCollector collector = null;
        private BoundingBoxXYZ intanceBox = new();
        private BoundingBoxXYZ hostBox = new();

        private IDictionary<XYZ, Element> tempDict { get; set; } = null;
        private ConcurrentDictionary<string, ElementTypeData> dictDatabase { get; set; } = ElementDataDictionary.ElementTypeSizeDictionary;

        private readonly double minimum = 0;
        private double angleRadians = 0;
        private double angleHorisontDegrees = 0;
        private double angleVerticalDegrees = 0;
        private double diameter = 0;
        private double hight = 0;
        private double widht = 0;

        #endregion

        private void InitializeUnits(Document doc)
        {
            dataBase = new();
            units = doc.GetUnits();
            angleUnit = units.GetFormatOptions(UnitType.UT_Angle).DisplayUnits;
        }


        public IEnumerable<ElementModel> GetCollisionByLevel(Document doc, Level level, DocumentModel source, Category category, IList<Element> elements)
        {
            InitializeUnits(doc);
            searchCatId = category.Id;
            searchDoc = source.Document;
            searchTrans = source.Transform;
            searchInstance = source.LinkInstance;
            levelIntId = level.Id.IntegerValue;
            foreach (Element host in elements)
            {
                if (levelIntId == host.LevelId.IntegerValue)
                {
                    foreach (ElementModel model in GetIntersectionElementModels(doc, searchCatId, searchTrans, host))
                    {
                        yield return model;
                    }

                }
            }
        }


        private IEnumerable<ElementModel> GetIntersectionElementModels(Document doc, ElementId catId, Transform transform, Element host)
        {
            int hostIdInt = host.Id.IntegerValue;
            tempDict = new Dictionary<XYZ, Element>(5);
            centroidPoint = host.GetMiddlePointByBoundingBox(ref hostBox);
            hostDirection = host is Wall wall ? wall.Orientation : XYZ.BasisZ;
            hostSolid = host.GetElementSolidByCenter(options, identityTransform, centroidPoint);
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBox.GetOutLine());
            LogicalAndFilter intersectFilter = new(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).WherePasses(intersectFilter).OfCategoryId(catId);
            foreach (Element element in collector)
            {
                if (hostSolid != null && VerifyElementByLenght(element))
                {
                    if (GetElementDirection(element, out intersectNormal) && IsParallel(hostDirection, intersectNormal))
                    {
                        intersectSolid = GetIntersectSolid(hostSolid, element, transform);
                        if (intersectSolid != null && intersectSolid.Volume > toleranceVolume)
                        {
                            if (IsValidElement(element, intersectSolid, hostDirection, out ElementTypeData sizeData))
                            {
                                yield return new ElementModel(element, sizeData, hostIdInt);
                            }
                        }
                    }
                }
            }
        }


        private bool IsValidElement(Element element, Solid intersectSolid, XYZ hostDirection, out ElementTypeData sizeData)
        {
            double distance;
            intanceBox = element.get_BoundingBox(null);
            centroidPoint = intersectSolid.ComputeCentroid();
            sizeData = DefineElementSize(element, intersectNormal);
            foreach (KeyValuePair<XYZ, Element> item in tempDict)
            {
                distance = centroidPoint.DistanceTo(item.Key);
                if (distance < minimum)
                {
                    
                    sizeData = GetSizeByBoundingBox(intanceBox, hostDirection);
                    return false;
                }
            }
            return true;
        }


        private ElementTypeData GetSizeByBoundingBox(BoundingBoxXYZ bbox, XYZ direction)
        {
            return new ElementTypeData(null);
        }



        //public bool CutOpening(IList<ElementId> hostIds)
        //{
        //    return false;
        //}


        private bool VerifyElementByLenght(Element elem)
        {
            return elem.Location is not LocationCurve || elem.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() > minimum;
        }


        private bool GetElementDirection(Element elem, out XYZ direction)
        {
            direction = XYZ.Zero;
            if (elem is FamilyInstance instance)
            {
                transform = instance.GetTransform();
                direction = transform.BasisX.Normalize();
            }
            else if (elem.Location is LocationCurve curve)
            {
                Line line = curve.Curve as Line;
                direction = line.Direction.Normalize();
            }
            return direction.IsAlmostEqualTo(XYZ.Zero) == false;
        }


        private Solid GetIntersectSolid(Solid hostSolid, Element elem, Transform transform = null)
        {
            intersectSolid = null;
            transform ??= identityTransform;
            double tolerance = toleranceVolume;
            GeometryElement geomElement = elem.get_Geometry(options);
            BooleanOperationsType unionType = BooleanOperationsType.Union;
            BooleanOperationsType intersectType = BooleanOperationsType.Intersect;
            foreach (GeometryObject geomObj in geomElement.GetTransformed(transform))
            {
                if (geomObj is Solid solid && solid.Faces.Size > 0)
                {
                    try
                    {
                        solid = BooleanOperationsUtils.ExecuteBooleanOperation(hostSolid, solid, intersectType);
                        if (intersectSolid != null && solid != null && solid.Faces.Size > 0)
                        {
                            solid = BooleanOperationsUtils.ExecuteBooleanOperation(intersectSolid, solid, unionType);
                        }
                    }
                    finally
                    {
                        if (solid != null && solid.Volume > tolerance)
                        {
                            tolerance = solid.Volume;
                            intersectSolid = solid;
                        }
                    }
                }
            }
            return intersectSolid;
        }


        private ElementTypeData DefineElementSize(Element elem, XYZ direction)
        {
            Document doc = elem.Document;
            ElementTypeData structData = new(null);
            if (doc.GetElement(elem.GetTypeId()) is ElementType etype)
            {
                unique = etype.UniqueId.Normalize();
                if (!dictDatabase.TryGetValue(unique, out structData))
                {
                    structData = GetSectionSize(elem, etype, direction);
                    if (dictDatabase.TryAdd(unique, structData))
                    {
                        dataBase.SerializeData(dictDatabase);
                    }
                }
            }
            return structData;
        }


        private ElementTypeData GetSectionSize(Element elem, ElementType etype, XYZ direction)
        {
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
                            return GetSizeByGeometry(etype, direction);
                        }
                }
            }
            return new ElementTypeData(null);
        }


        private ElementTypeData GetSizeByGeometry(ElementType etype, XYZ direction)
        {
            transform = identityTransform;
            ElementTypeData result = new(null);
            direction = ResetDirectionToPositive(direction);
            angleHorisontDegrees = ConvertRadiansToDegrees(GetHorizontAngleRadiansByNormal(direction));
            angleVerticalDegrees = ConvertRadiansToDegrees(GetVerticalAngleRadiansByNormal(direction));
            Transform horizont = Transform.CreateRotationAtPoint(identityTransform.BasisZ, GetInternalAngle(angleHorisontDegrees), centroidPoint);
            Transform vertical = Transform.CreateRotationAtPoint(identityTransform.BasisX, GetInternalAngle(angleVerticalDegrees), centroidPoint);
            intersectSolid = angleHorisontDegrees == 0 ? intersectSolid : SolidUtils.CreateTransformed(intersectSolid, horizont);
            intersectSolid = angleVerticalDegrees == 0 ? intersectSolid : SolidUtils.CreateTransformed(intersectSolid, vertical);
            BoundingBoxXYZ bbox = intersectSolid?.GetBoundingBox();
            if (bbox != null)
            {
                widht = Math.Abs(bbox.Max.X - bbox.Min.X);
                hight = Math.Abs(bbox.Max.Z - bbox.Min.Z);
                result = new ElementTypeData(etype, hight, widht);
                bbox.Dispose();
            }
            return result;
        }


        private bool IsParallel(XYZ normal, XYZ direction)
        {
            return !direction.IsAlmostEqualTo(XYZ.Zero) && Math.Abs(normal.DotProduct(direction)) > thresholdAngle;
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
        //    double value = invalIdInt;
        //    int minimum = int.MaxValue;
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
        //                if (minimum > tmp && UnitFormatUtils.TryParse(units, UnitType.UT_Length, param.AsValueString(), out value))
        //                {
        //                    minimum = tmp;
        //                }
        //            }
        //        }
        //    }
        //    return value;
        //}


        //private void CreateDirectShape(Document doc, Element elem, Solid solid)
        //{
        //    using Transaction trans = new(doc, "Create DirectShape");
        //    try
        //    {
        //        _ = trans.Start();
        //        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
        //        ds.ApplicationDataId = elem.UniqueId;
        //        ds.Name = "Intersection by " + elem.Name;
        //        ds.SetShape(new GeometryObject[] { solid });
        //        _ = trans.Commit();
        //    }
        //    catch (Exception exc)
        //    {
        //        _ = trans.RollBack();
        //        Logger.Error(exc.Message);
        //    }
        //}


        //private bool GetFamilyInstanceReferencePlane(FamilyInstance fi, out XYZ origin, out XYZ direction)
        //{
        //    bool flag = false;
        //    origin = XYZ.Zero;
        //    direction = XYZ.Zero;

        //    Reference reference = fi.GetReferences(FamilyInstanceReferenceType.CenterFrontBack).FirstOrDefault();
        //    reference = searchInstance != null ? reference.CreateLinkReference(searchInstance) : reference;

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
        //                direction = plan.Normal;
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


        //private double GetRotationAngleFromTransform(Transform transform)
        //{
        //    double x = transform.BasisX.X;
        //    double y = transform.BasisY.Y;
        //    double z = transform.BasisZ.Z;
        //    double trace = x + y + z;
        //    return Math.Acos((trace - 1) / 2.0);
        //}


        //private static IEnumerable<CurveLoop> GetCountours(Solid solid, Element elem)
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
            dictDatabase.Clear();
            transform?.Dispose();
            searchDoc?.Dispose();
            intersectSolid?.Dispose();
            searchInstance?.Dispose();
            searchTrans?.Dispose();

            Logger.Log(dictDatabase.Values.Count.ToString());
        }
    }
}
