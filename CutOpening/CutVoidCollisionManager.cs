using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;
using Level = Autodesk.Revit.DB.Level;
using Line = Autodesk.Revit.DB.Line;
using Material = Autodesk.Revit.DB.Material;

namespace RevitTimasBIMTools.CutOpening
{
    public sealed class CutVoidCollisionManager
    {

        #region Default Properties

        private const double footToMm = 304.8;
        private readonly Options options = new()
        {
            ComputeReferences = true,
            IncludeNonVisibleObjects = true,
            DetailLevel = ViewDetailLevel.Medium
        };

        private readonly Transform identity = Transform.Identity;
        private readonly SolidCurveIntersectionOptions intersectOptions = new();
        private readonly double threshold = Math.Round(Math.Cos(Math.PI / 4), 5);

        #endregion


        #region Input Members

        private Document searchDocument { get; set; } = null;
        private Transform searchTransform { get; set; } = null;
        private IDictionary<int, ElementId> ElementTypeIdData { get; set; } = null;

        //private readonly string widthParamName = "ширина";
        //private readonly string heightParamName = "высота";

        #endregion


        #region Fields

        private double cutOffset;
        private double minSideSize;

        private FilteredElementCollector collector;

        private XYZ centroid = null;

        private Solid hostSolid = null;
        private XYZ direction = XYZ.BasisZ;
        private XYZ hostNormal = XYZ.BasisZ;
        private Line line = null;
        private Solid intersectionSolid = null;


        private BoundingBoxXYZ hostBbox = null;
        private BoundingBoxXYZ intersectionBbox = null;

        #endregion


        public void InitializeElementTypeIdData(Document doc)
        {
            ElementTypeIdData = RevitPurginqManager.PurgeAndGetValidConstructionTypeIds(doc);
        }


        public IDictionary<string, Material> GetStructureCoreMaterialData(Document doc)
        {
            return ElementTypeIdData.GetStructureCoreMaterialData(doc);
        }


        public IList<ElementModel> GetCollisionByInputData(Document doc, DocumentModel document, Material material, Category category)
        {
            Transform global = document.Transform;
            IList<ElementModel> output = new List<ElementModel>(50);
            cutOffset = Convert.ToDouble(Properties.Settings.Default.CutOffsetInMm / footToMm);
            minSideSize = Convert.ToDouble(Properties.Settings.Default.MinSideSizeInMm / footToMm);
            IEnumerable<Element> enclosures = ElementTypeIdData?.GetInstancesByTypeIdDataAndMaterial(doc, material);
            using TransactionGroup transGroup = new(doc, "GetCollision");
            TransactionStatus status = transGroup.Start();
            foreach (Element host in enclosures)
            {
                foreach (ElementModel model in GetIntersectionByElement(doc, host, global, category))
                {
                    output.Add(model);
                }
            }
            status = transGroup.Assimilate();
            return output;
        }


        private IEnumerable<ElementModel> GetIntersectionByElement(Document doc, Element host, Transform global, Category category)
        {
            hostBbox = host.get_BoundingBox(null);
            hostNormal = host.GetHostElementNormal();
            hostSolid = host.GetSolidByVolume(identity, options);
            Level level = doc.GetElement(host.LevelId) as Level;
            // ElementIntersectsElementFilter можно попоробоювать поменять
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBbox.GetOutLine());
            LogicalAndFilter intersectFilter = new(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).OfCategoryId(category.Id).WherePasses(intersectFilter);
            foreach (Element elem in collector)
            {
                centroid = elem.GetMiddlePointByBoundingBox(out intersectionBbox);
                if (IsIntersectionValid(elem, hostSolid, hostNormal, centroid, threshold, out direction, out line))
                {
                    intersectionSolid = hostSolid.GetIntersectionSolid(elem, global, options);
                    intersectionBbox = intersectionSolid.GetBoundingBox();
                    centroid = intersectionSolid.ComputeCentroid();

                    ElementModel model = new(elem, level)
                    {
                        Origin = centroid,
                        Direction = direction,
                        HostNormal = hostNormal,
                    };

                    if (GetSectionSize(doc, ref model, direction, centroid))
                    {
                        CalculateOpeningSize(ref model, line, cutOffset);
                        //intersectionSolid = intersectionSolid.ScaledSolidByOffset(centroid, intersectionBbox, cutOffset);
                        yield return model;
                    }
                }
            }
        }


        private bool IsIntersectionValid(Element elem, in Solid solid, in XYZ normal, in XYZ centroid, in double threshold, out XYZ direction, out Line line)
        {
            line = null;
            bool result = false;
            direction = XYZ.Zero;
            if (elem.Location is LocationCurve curve)
            {
                line = curve.Curve as Line;
                direction = line.Direction.Normalize();
                IsIntersectionValid(solid, ref line);
                result = true;
            }
            else if (elem is FamilyInstance instance)
            {
                Transform transform = instance.GetTransform();
                if (normal.IsAlmostEqualTo(transform.BasisX.Normalize(), threshold))
                {
                    direction = transform.BasisX.Normalize();
                    line = CreateLine(direction, centroid);
                    IsIntersectionValid(solid, ref line);
                    result = true;
                }
                if (normal.IsAlmostEqualTo(transform.BasisY.Normalize(), threshold))
                {
                    direction = transform.BasisY.Normalize();
                    line = CreateLine(direction, centroid);
                    IsIntersectionValid(solid, ref line);
                    result = true;
                }
            }
            return result;
        }


        Line CreateLine(XYZ direction, XYZ centroid)
        {
            XYZ strPnt = centroid - (direction * 3);
            XYZ endPnt = centroid + (direction * 3);
            return Line.CreateBound(strPnt, endPnt);
        }


        void IsIntersectionValid(in Solid solid, ref Line line)
        {
            SolidCurveIntersection curves = solid.IntersectWithCurve(this.line, intersectOptions);
            if (curves != null && 0 < curves.SegmentCount)
            {
                line = curves.GetCurveSegment(0) as Line;
            }

        }


        private bool GetSectionSize(Document doc, ref ElementModel model, in XYZ direction, in XYZ centroid)
        {
            BoundingBoxUV size = intersectionSolid.GetSectionBound(doc, in direction, in centroid);
            if (direction.IsAlmostEqualTo(XYZ.BasisX, 0.5))
            {
                model.Width = Math.Round(size.Max.U - size.Min.U, 5);
                model.Height = Math.Round(size.Max.V - size.Min.V, 5);
            }
            else
            {
                model.Width = Math.Round(size.Max.V - size.Min.V, 5);
                model.Height = Math.Round(size.Max.U - size.Min.U, 5);
            }
            return model.SetSizeDescription();
        }


        private void CalculateOpeningSize(ref ElementModel model, in Line line, double offset)
        {
            if (Math.Min(model.Width, model.Height) > minSideSize)
            {
                if (!model.HostNormal.IsParallel(model.Direction))
                {
                    XYZ vector = line.GetEndPoint(1) - line.GetEndPoint(0);
                    model.HostNormal.GetAngleBetween(direction, out double horizont, out double vertical);
                    double hostDeph = Math.Abs(model.HostNormal.DotProduct(vector));
                    model.Height += offset + CalculateSideSize(hostDeph, vertical);
                    model.Width += offset + CalculateSideSize(hostDeph, horizont);
                }
            }
        }


        private double CalculateSideSize(double hostDeph, in double angle)
        {
            hostDeph = Math.Round(hostDeph * 304.8, MidpointRounding.AwayFromZero) / 304.8;
            return Math.Round(Math.Tan(angle * hostDeph), 5);
        }


        public void CreateOpening(Document doc, ElementModel model, FamilySymbol wallOpenning, FamilySymbol floorOpenning, Definition definition = null, double offset = 0)
        {
            FamilyInstance opening = null;
            using Transaction trans = new(doc, "Create opening");
            TransactionStatus status = trans.Start();
            if (status == TransactionStatus.Started)
            {
                Element instanse = model.Instanse;
                try
                {
                    if (instanse is Wall wall && wall.IsValidObject)
                    {
                        opening = doc.Create.NewFamilyInstance(model.Origin, wallOpenning, model.HostLevel, StructuralType.NonStructural);
                    }
                    if (instanse is RoofBase roof && roof.IsValidObject)
                    {
                        opening = doc.Create.NewFamilyInstance(model.Origin, floorOpenning, model.HostLevel, StructuralType.NonStructural);
                    }
                    if (instanse is Floor floor && floor.IsValidObject)
                    {
                        opening = doc.Create.NewFamilyInstance(model.Origin, floorOpenning, model.HostLevel, StructuralType.NonStructural);
                    }
                    if (opening != null)
                    {
                        _ = opening.get_Parameter(definition).Set(model.Width + (offset * 2));
                        _ = opening.get_Parameter(definition).Set(model.Height + (offset * 2));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
                finally
                {
                    status = trans.Commit();
                }
            }
        }


        //private bool CheckSizeOpenning(Document doc, BoundingBoxXYZ bbox, XYZ direction, View view)
        //{
        //    Outline outline = new(bbox.Min -= offsetPnt, bbox.Max += offsetPnt);
        //    collector = new FilteredElementCollector(doc, view.Id).Excluding(idsExclude);
        //    instanceId = collector.WherePasses(new BoundingBoxIntersectsFilter(outline)).FirstElementId();
        //    return instanceId == null;
        //}


        #region Other methods


        //private bool ComputeIntersectionVolume(Solid solidA, Solid solidB)
        //{
        //    Solid line = BooleanOperationsUtils.ExecuteBooleanOperation(solidA, solidB, BooleanOperationsType.Intersect);
        //    return line.Volume > 0;
        //}



        //private double GetLengthValueBySimilarParameterName(Instance floor, string paramName)
        //{
        //    double value = invalidInt;
        //    int minDistance = int.MaxValue;
        //    char[] delimiters = new[] { ' ', '_', '-' };
        //    foreach (Parameter param in floor.GetOrderedParameters())
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


        //private bool GetFamilyInstanceReferencePlane(FamilyInstance fi, out XYZ origin, out XYZ direction)
        //{
        //    bool flag = false;
        //    origin = XYZ.Zero;
        //    direction = XYZ.Zero;

        //    Reference reference = fi.GetReferences(FamilyInstanceReferenceType.CenterFrontBack).FirstOrDefault();
        //    reference = SearchInstance != null ? reference.CreateLinkReference(SearchInstance) : reference;

        //    if (null != reference)
        //    {
        //        Document doc = fi.Document;
        //        using Transaction trans = new(doc);
        //        _ = trans.Start("Create Temporary Sketch Plane");
        //        try
        //        {
        //            SketchPlane sketchPlan = SketchPlane.Create(doc, reference);
        //            if (null != sketchPlan)
        //            {
        //                Plane plan = sketchPlan.GetPlane();
        //                direction = plan.Normal;
        //                origin = plan.Origin;
        //                flag = true;
        //            }
        //        }
        //        finally
        //        {
        //            _ = trans.RollBack();
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

        //private void УxtractSectionSize(Element floor)
        //{
        //    hight = 0; widht = 0;
        //    int catIdInt = floor.Category.Id.IntegerValue;
        //    if (floor.Document.GetElement(floor.GetTypeId()) is ElementType)
        //    {
        //        BuiltInCategory builtInCategory = (BuiltInCategory)catIdInt;
        //        switch (builtInCategory)
        //        {
        //            case BuiltInCategory.OST_PipeCurves:
        //                {
        //                    diameter = floor.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
        //                    return;
        //                }
        //            case BuiltInCategory.OST_DuctCurves:
        //                {
        //                    Parameter diameterParam = floor.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
        //                    if (diameterParam != null && diameterParam.HasValue)
        //                    {
        //                        diameter = diameterParam.AsDouble();
        //                        hight = diameter;
        //                        widht = diameter;
        //                    }
        //                    else
        //                    {
        //                        hight = floor.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
        //                        widht = floor.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
        //                    }
        //                    return;
        //                }
        //            case BuiltInCategory.OST_Conduit:
        //                {
        //                    diameter = floor.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).AsDouble();
        //                    return;
        //                }
        //            case BuiltInCategory.OST_CableTray:
        //                {
        //                    hight = floor.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();
        //                    widht = floor.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
        //                    return;
        //                }
        //            default:
        //                {
        //                    return;
        //                }
        //        }
        //}

        #endregion


        [STAThread]
        public void Dispose()
        {
            hostSolid?.Dispose();
            intersectionSolid?.Dispose();
            searchDocument?.Dispose();
            searchTransform?.Dispose();
        }
    }
}
