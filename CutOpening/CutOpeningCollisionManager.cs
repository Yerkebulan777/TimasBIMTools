using Autodesk.Revit.DB;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Document = Autodesk.Revit.DB.Document;

namespace RevitTimasBIMTools.CutOpening
{
    internal sealed class CutOpeningCollisionManager : IDisposable
    {
        #region Static members
        private static Units units = null;
        private static DisplayUnitType angleUnit;
        private readonly Options options = GetGeometryOptions();
        private static readonly char[] delimiters = new[] { ' ', '_', '-' };
        private static readonly ParameterType lenParamType = ParameterType.Length;
        private static readonly int sizeReserveInMm = Properties.Settings.Default.CutOffsetInMm;
        private static readonly ElementMulticategoryFilter multicategoryFilter = GetMulticategoryFilter();
        private static CancellationToken cancelToken = CutOpeningDataViewModel.CancelToken;

        private static Options GetGeometryOptions()
        {
            return new Options
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = false,
                DetailLevel = ViewDetailLevel.Medium
            };
        }

        private static ElementMulticategoryFilter GetMulticategoryFilter()
        {
            List<BuiltInCategory> builtInCats = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Conduit,
                BuiltInCategory.OST_Furniture,
                BuiltInCategory.OST_CableTray,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_MechanicalEquipment
            };
            return new ElementMulticategoryFilter(builtInCats);
        }
        #endregion

        private bool canceled = false;

        private const int invalIdInt = -1;
        private const double footToMm = 304.8;
        private const double toleranceVolume = 0.0005;
        private const double rightAngle = Math.PI / 2;
        private const string widthParamName = "ширина";
        private const string heightParamName = "высота";

        private Document linkDocument = null;
        private Document currentDocument = null;
        private Transform linkDocTransform = null;
        private RevitLinkInstance linkInstance = null;
        private FilteredElementCollector collector = null;

        private readonly ElementId invalId = ElementId.InvalidElementId;
        private readonly Transform identityTransform = Transform.Identity;
        private readonly StringBuilder stringBuilder = new StringBuilder(25);
        private readonly CopyPasteOptions copyOptions = new CopyPasteOptions();
                
        private const double minWidthSize = 150 / footToMm;
        private readonly IList<ElementId> hostIdList = new List<ElementId>(150);

        private readonly int minSideSize = Properties.Settings.Default.MinSideSize;
        private readonly int maxSideSize = Properties.Settings.Default.MaxSideSize;
        private readonly double thresholdAngle = Math.Round(Math.Cos(45 * Math.PI / 180), 5);
        private readonly IList<RevitElementModel> modelList = new List<RevitElementModel>(300);
        private readonly string linkDocumentTitle = Properties.Settings.Default.TargetDocumentName;
        private readonly ConcurrentDictionary<string, ElementTypeData> dictDatabase = ElementDataDictionary.ElementTypeSizeDictionary;


        #region Templory properties

        private XYZ centroidPoint = XYZ.Zero;
        private XYZ commDirection = XYZ.BasisZ;
        private XYZ hostDirection = XYZ.BasisZ;


        private Solid hostSolid = null;
        private Solid intersectSolid = null;
        private string uniqueKey = string.Empty;
        private ElementId elemId = ElementId.InvalidElementId;
        private BoundingBoxXYZ hostBbox = new BoundingBoxXYZ();
        private Transform transform = Transform.Identity;

        private double angleRadians = 0;
        private double angleHorisontDegrees = 0;
        private double angleVerticalDegrees = 0;
        private double diameter = 0;
        private double hight = 0;
        private double widht = 0;

        #endregion


        [STAThread]
        public void InitializeActiveDocument(Document document)
        {
            currentDocument = document;
            units = document.GetUnits();
            GetTargetRevitLinkInstance(document);
            Properties.Settings.Default.Upgrade();
            angleUnit = units.GetFormatOptions(UnitType.UT_Angle).DisplayUnits;
        }


        [STAThread]
        public IList<RevitElementModel> GetCollisionCommunicateElements()
        {
            modelList.Clear();
            collector = ValidWallCollector();
            foreach (Element host in collector)
            {
                canceled = cancelToken.IsCancellationRequested;
                if (false == canceled && host is Wall wall)
                {
                    hostDirection = wall.Orientation;
                    centroidPoint = host.GetMiddlePointByBoundingBox(ref hostBbox);
                    hostSolid = host.GetElementCenterSolid(options, identityTransform, centroidPoint);
                    if (hostSolid != null)
                    {
                        foreach (RevitElementModel model in GetIntersectionElementModels(currentDocument))
                        {
                            elemId = host.Id;
                            if (!hostIdList.Contains(elemId))
                            {
                                hostIdList.Add(elemId);
                            }
                            modelList.Add(model);
                        }
                    }
                }
            }
            return modelList;
        }


        public bool CutOpening(IList<ElementId> hostIds)
        {
            return false;
        }


        private void GetTargetRevitLinkInstance(Document document)
        {
            currentDocument = document;
            if (!string.IsNullOrEmpty(linkDocumentTitle))
            {
                if (linkDocumentTitle.Equals(currentDocument.Title.Trim()))
                {
                    linkDocTransform = Transform.Identity;
                    linkDocument = currentDocument;
                    linkInstance = null;
                }
                collector = new FilteredElementCollector(currentDocument);
                collector = collector.OfCategory(BuiltInCategory.OST_RvtLinks).OfClass(typeof(RevitLinkInstance));
                foreach (Element elem in collector)
                {
                    if (elem is RevitLinkInstance instance)
                    {
                        Document doc = instance.GetLinkDocument();
                        if (doc.IsLinked && linkDocumentTitle == doc.Title.Trim())
                        {
                            linkDocTransform = instance.GetTotalTransform();
                            linkInstance = instance;
                            linkDocument = doc;
                        }
                    }
                }
            }
        }


        private FilteredElementCollector ValidWallCollector()
        {
            BuiltInCategory bic = BuiltInCategory.OST_Walls;
            ElementId paramId = new ElementId(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);
            collector = RevitFilterManager.GetInstancesOfCategory(currentDocument, typeof(Wall), bic);
            collector = RevitFilterManager.ParamFilterFactory(collector, paramId, minWidthSize, 1);
            collector = collector.WherePasses(new ElementLevelFilter(invalId, true));
            return collector;
        }


        private IEnumerable<RevitElementModel> GetIntersectionElementModels(Document doc)
        {
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBbox.GetOutLine());
            LogicalAndFilter intersectFilter = new LogicalAndFilter(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).WherePasses(intersectFilter);
            collector = collector.WherePasses(multicategoryFilter);
            foreach (Element elem in collector)
            {
                if (elem.IsValidObject && VerifyElementByLenght(elem))
                {
                    if (GetElementDirection(elem, out commDirection) && IsParallel(hostDirection, commDirection))
                    {
                        intersectSolid = GetIntersectSolid(hostSolid, elem, linkDocTransform);
                        if (intersectSolid != null && intersectSolid.Volume > toleranceVolume)
                        {
                            centroidPoint = intersectSolid.ComputeCentroid();
                            ElementTypeData sizeData = DefineElementSize(elem);
                            yield return new RevitElementModel(elem, sizeData, stringBuilder.ToString());
                        }
                    }
                }
            }
        }


        private bool VerifyElementByLenght(Element elem)
        {
            return !(elem.Location is LocationCurve) || elem.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() > minWidthSize;
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
                Curve line = curve.Curve;
                direction = (line.GetEndPoint(0) - line.GetEndPoint(1)).Normalize();
            }
            return direction.IsAlmostEqualTo(XYZ.Zero) == false;
        }


        private bool IsParallel(XYZ normal, XYZ direction)
        {
            return !direction.IsAlmostEqualTo(XYZ.Zero) && Math.Abs(normal.DotProduct(direction)) > thresholdAngle;
        }


        private Solid GetIntersectSolid(Solid hostSolid, Element elem, Transform transform = null)
        {
            intersectSolid = null;
            double tolerance = toleranceVolume;
            transform = transform ?? identityTransform;
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


        private ElementTypeData DefineElementSize(Element elem)
        {
            Document doc = elem.Document;
            if (doc.GetElement(elem.GetTypeId()) is ElementType etype && etype.IsValidObject)
            {
                uniqueKey = etype.UniqueId;
                //if (dictDatabase.TryGetValue(uniqueKey, out structData) && structData.IsValidDataObject)
                //{
                //    return structData;
                //}
                //structData = GetSizeByParameter(hostInstance, etype);
                //if (structData.IsValidDataObject && dictDatabase.TryAdd(uniqueKey.Normalize(), structData))
                //{
                //    return structData;
                //}
                object result = GetSizeByGeometry(etype, elem, commDirection);
                //stringBuilder.AppendLine($"Solid volume {hostSolid.Volume} {hostDirection} {commDirection}");
                if (result is ElementTypeData data && data.IsValidObject)
                {
                    _ = dictDatabase.TryAdd(uniqueKey, data);
                    return data;
                }
            }
            return new ElementTypeData(null);
        }


        private object GetSizeByParameter(Element elem, ElementType etype)
        {
            object resultData = null;
            if (elem.IsValidObject)
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
                                widht = GetLengthValueBySimilarParameterName(elem, widthParamName);
                                hight = GetLengthValueBySimilarParameterName(elem, heightParamName);
                                return new ElementTypeData(etype, hight, widht);
                            }
                    }
                }
            }
            return resultData;
        }


        private double GetLengthValueBySimilarParameterName(Element elem, string paramName)
        {
            double value = invalIdInt;
            int tolerance = int.MaxValue;
            foreach (Parameter param in elem.GetOrderedParameters())
            {
                Definition definition = param.Definition;
                if (param.HasValue && definition.ParameterType == lenParamType)
                {
                    string name = definition.Name;
                    string[] strArray = name.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (strArray.Contains(paramName, StringComparer.CurrentCultureIgnoreCase))
                    {
                        int tmp = param.IsShared ? name.Length : name.Length + strArray.Length;
                        if (tolerance > tmp && UnitFormatUtils.TryParse(units, UnitType.UT_Length, param.AsValueString(), out value))
                        {
                            tolerance = tmp;
                        }
                    }
                }
            }
            return value;
        }


        private object GetSizeByGeometry(ElementType etype, Element elem, XYZ direction)
        {
            object result = null;
            _ = stringBuilder.Clear();
            transform = identityTransform;
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
                widht = RoundSize(Math.Abs(bbox.Max.X - bbox.Min.X));
                hight = RoundSize(Math.Abs(bbox.Max.Z - bbox.Min.Z));
                result = new ElementTypeData(etype, hight, widht);
                CreateDirectShape(currentDocument, elem, intersectSolid);
                bbox.Dispose();
            }
            return result;
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


        private double RoundSize(double value, int digit = 5)
        {
            return Math.Round(value * footToMm / digit, MidpointRounding.AwayFromZero) * digit / footToMm;
        }









        #region Other methods
        private void CreateDirectShape(Document doc, Element elem, Solid solid)
        {
            using (Transaction trans = new Transaction(doc, "Create DirectShape"))
            {
                try
                {
                    _ = trans.Start();
                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                    ds.ApplicationDataId = elem.UniqueId;
                    ds.Name = "Intersection by " + elem.Name;
                    ds.SetShape(new GeometryObject[] { solid });
                    _ = trans.Commit();
                }
                catch (Exception exc)
                {
                    _ = trans.RollBack();
                    RevitLogger.Error(exc.Message);
                }
            }
        }


        private bool GetFamilyInstanceReferencePlane(FamilyInstance fi, out XYZ origin, out XYZ direction)
        {
            bool flag = false;
            origin = XYZ.Zero;
            direction = XYZ.Zero;

            Reference reference = fi.GetReferences(FamilyInstanceReferenceType.CenterFrontBack).FirstOrDefault();
            reference = linkInstance != null ? reference.CreateLinkReference(linkInstance) : reference;

            if (null != reference)
            {
                Document doc = fi.Document;
                using (Transaction transaction = new Transaction(doc))
                {
                    _ = transaction.Start("Create Temporary Sketch Plane");
                    try
                    {
                        SketchPlane sketch = SketchPlane.Create(doc, reference);
                        if (null != sketch)
                        {
                            Plane plan = sketch.GetPlane();
                            direction = plan.Normal;
                            origin = plan.Origin;
                            flag = true;
                        }
                    }
                    finally
                    {
                        _ = transaction.RollBack();
                    }
                }
            }
            return flag;
        }


        private double GetRotationAngleFromTransform(Transform transform)
        {
            double x = transform.BasisX.X;
            double y = transform.BasisY.Y;
            double z = transform.BasisZ.Z;
            double trace = x + y + z;
            return Math.Acos((trace - 1) / 2.0);
        }


        private static IEnumerable<CurveLoop> GetCountours(Solid solid, Element elem)
        {
            try
            {
                Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, elem.get_BoundingBox(null).Min);
                ExtrusionAnalyzer analyzer = ExtrusionAnalyzer.Create(solid, plane, XYZ.BasisZ);
                Face face = analyzer.GetExtrusionBase();
                return face.GetEdgesAsCurveLoops();
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                return Enumerable.Empty<CurveLoop>();
            }
        }

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
         * Отдельный класс
         * Первая проверка остается
         * Вторая проверка в цикле выполнения
         * Объединения елементов в одной точке в один большой bbox если они пересекаются
         * Объединения проема если пересекаются bbox или находятся очень близко
         * Создать новое семейство в цикле с возможностью изменения размеров
         * Сoхранение состояния в цикле по ходу выполнения с отменой действия
         * В цикле открывается окно с видом в плане и настройками проема (настройки размеров сохраняются по ходу)
         * TransGroup и subtransaction с doc regenerate и предосмотр с закрытием окна
         * Реализовать автосинхронизацию при окончания выполнение или остановке
         * Кнопки = (продолжить/создать/остановить)
         * Необходимо использовать Dispose()
         */


        public void Dispose()
        {
            modelList.Clear();
            transform?.Dispose();
            collector?.Dispose();
            stringBuilder.Clear();
            linkInstance?.Dispose();
            linkDocument?.Dispose();
            intersectSolid?.Dispose();
            currentDocument?.Dispose();
            linkDocTransform?.Dispose();
            ElementDataDictionary.OnSerializeData(dictDatabase);
        }
    }
}
