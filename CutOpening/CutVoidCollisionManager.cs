using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
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

        private int minSideSize;
        private double cutOffset;

        private FilteredElementCollector collector;

        private XYZ centroid = null;

        private Solid hostSolid = null;
        private XYZ vector = XYZ.BasisZ;
        private XYZ hostNormal = XYZ.BasisZ;
        private Solid intersectSolid = null;


        private BoundingBoxXYZ hostBbox = null;
        private BoundingBoxXYZ intersectBbox = null;
        private IList<CurveLoop> curveloops = null;
        private double width = 0;
        private double height = 0;

        #endregion


        public void InitializeElementTypeIdData(Document doc)
        {
            ElementTypeIdData = RevitPurginqManager.PurgeAndGetValidConstructionTypeIds(doc);
        }


        public IDictionary<string, Material> GetStructureCoreMaterialData(Document doc)
        {
            return ElementTypeIdData.GetStructureCoreMaterialData(doc);
        }


        #region Get Collision Data

        public IList<ElementModel> GetCollisionByInputData(Document doc, DocumentModel document, Material material, Category category)
        {
            Transform global = document.Transform;
            IList<ElementModel> output = new List<ElementModel>(50);
            minSideSize = Properties.Settings.Default.MinSideSizeInMm;
            cutOffset = Convert.ToDouble(Properties.Settings.Default.CutOffsetInMm / footToMm);
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
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBbox.GetOutLine());
            LogicalAndFilter intersectFilter = new(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).OfCategoryId(category.Id).WherePasses(intersectFilter);
            foreach (Element elem in collector)
            {
                centroid = elem.GetMiddlePointByBoundingBox(out intersectBbox);
                if (IsIntersectionValid(elem, hostSolid, hostNormal, centroid, out vector))
                {
                    intersectSolid = hostSolid.GetIntersectionSolid(elem, global, options);
                    intersectBbox = intersectSolid.GetBoundingBox();
                    centroid = intersectSolid.ComputeCentroid();

                    curveloops = intersectSolid.GetSectionSize(doc, centroid, hostNormal, out width, out height);
                    int minSize = Convert.ToInt16(Math.Round(Math.Min(width, height) * 304.8));
                    if (minSize > minSideSize)
                    {
                        ElementModel model = new(elem, level)
                        {
                            Width = width,
                            Height = height,
                            Vector = vector,
                            Origin = centroid,
                            Normal = hostNormal,
                            CurveLoops = curveloops,
                            MinSizeValue = minSize,
                        };
                        model.SetSizeDescription();
                        yield return model;
                    }
                }
            }
        }

        #endregion


        #region Validate Intersection
        private bool IsIntersectionValid(Element elem, in Solid solid, in XYZ normal, in XYZ centroid, out XYZ vector)
        {
            Line line = null;
            vector = XYZ.Zero;
            if (elem.Location is LocationCurve curve)
            {
                line = curve.Curve as Line;
                if (normal.IsAlmostEqualTo(line.Direction, threshold))
                {
                    vector = line.Direction.Normalize();
                }
            }
            else if (elem is FamilyInstance instance)
            {
                Transform transform = instance.GetTransform();

                if (normal.IsAlmostEqualTo(transform.BasisX, threshold))
                {
                    vector = transform.BasisX.Normalize();
                    line = CreateLine(vector, centroid);
                }

                if (normal.IsAlmostEqualTo(transform.BasisY, threshold))
                {
                    vector = transform.BasisY.Normalize();
                    line = CreateLine(vector, centroid);
                }
            }

            return GetIntersectionVector(solid, ref line, ref vector);
        }


        private bool GetIntersectionVector(in Solid solid, ref Line line, ref XYZ vector)
        {
            if (solid != null && line != null)
            {
                SolidCurveIntersection curves = solid.IntersectWithCurve(line, intersectOptions);
                if (curves != null && 0 < curves.SegmentCount)
                {
                    line = curves.GetCurveSegment(0) as Line;
                    vector = line.GetEndPoint(1) - line.GetEndPoint(0);
                }
            }
            return !vector.IsAlmostEqualTo(XYZ.Zero);
        }


        private Line CreateLine(XYZ direction, XYZ centroid)
        {
            XYZ strPnt = centroid - (direction * 3);
            XYZ endPnt = centroid + (direction * 3);
            return Line.CreateBound(strPnt, endPnt);
        }

        #endregion


        public void VerifyOpenningSize(Document doc, in ElementModel model, in double offset)
        {
            Solid solid = model.CurveLoops.CreateExtrusionGeometry(model.Normal, model.Depth, offset);
            using Transaction trans = new(doc, "Create opening");
            TransactionStatus status = trans.Start();
            if (status == TransactionStatus.Started)
            {
                solid.CreateDirectShape(doc);
                status = trans.Commit();
            }
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
                        CalculateOpeningSize(ref model, offset, out double width, out double height);
                        _ = opening.get_Parameter(definition).Set(width);
                        _ = opening.get_Parameter(definition).Set(height);
                    }
                }
                catch (Exception ex)
                {
                    status = trans.RollBack();
                    Logger.Error(ex.Message);
                }
                finally
                {
                    status = trans.Commit();
                }
            }
        }


        private void CalculateOpeningSize(ref ElementModel model, double offset, out double width, out double height)
        {
            width = model.Width + (offset * 2);
            height = model.Height + (offset * 2);
            if (!model.Normal.IsParallel(model.Vector))
            {
                model.Normal.GetAngleBetween(model.Vector, out double horizont, out double vertical);
                height += CalculateSideSize(model.Depth, vertical);
                width += CalculateSideSize(model.Depth, horizont);
            }
        }


        private double CalculateSideSize(in double hostDeph, in double angle)
        {
            return Math.Round(Math.Tan(angle * hostDeph), 5);
        }


        //private bool CheckSizeOpenning(Document doc, BoundingBoxXYZ bbox, XYZ vector, View view)
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


        //private bool GetFamilyInstanceReferencePlane(FamilyInstance fi, out XYZ origin, out XYZ vector)
        //{
        //    bool flag = false;
        //    origin = XYZ.Zero;
        //    vector = XYZ.Zero;

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
        //                vector = plan.Normal;
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
        //    height = 0; widht = 0;
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
        //                        height = diameter;
        //                        widht = diameter;
        //                    }
        //                    else
        //                    {
        //                        height = floor.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
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
        //                    height = floor.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();
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
            intersectSolid?.Dispose();
            searchDocument?.Dispose();
            searchTransform?.Dispose();
        }
    }
}
