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
using Plane = Autodesk.Revit.DB.Plane;

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

        private double minSideSize;
        //private readonly string widthParamName = "ширина";
        //private readonly string heightParamName = "высота";

        #endregion


        #region Fields

        private FilteredElementCollector collector;

        private XYZ centroid = null;
        private Solid hostSolid = null;
        private XYZ vector = XYZ.BasisZ;
        private XYZ hostNormal = XYZ.BasisZ;
        private BoundingBoxXYZ hostBbox = null;
        private BoundingBoxXYZ elemBbox = null;
        private Plane plane = null;
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
            minSideSize = Properties.Settings.Default.MinSideSizeInMm / footToMm;
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
            hostNormal = host.GetHostNormal();
            hostSolid = host.GetSolidByVolume(identity, options);
            Level level = doc.GetElement(host.LevelId) as Level;
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBbox.GetOutLine());
            LogicalAndFilter intersectFilter = new(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).OfCategoryId(category.Id).WherePasses(intersectFilter);

            foreach (Element elem in collector)
            {
                centroid = elem.GetMiddlePointByBoundingBox(out elemBbox);
                if (IsIntersectionValid(elem, hostSolid, hostNormal, centroid, out vector))
                {
                    ISet<XYZ> points = hostSolid.GetIntersectionPoints(elem, global, options, ref centroid);

                    plane = CreatePlaneByNormalAndCentroid(doc, hostNormal, centroid);

                    BoundingBoxUV sectionBox = plane.ProjectPointsOnPlane(points);

                    GetSectionSize(sectionBox, ref hostNormal, out width, out height);

                    double minSize = Math.Min(width, height);
                    if (minSize >= minSideSize)
                    {
                        ElementModel model = new(elem, level)
                        {
                            Width = width,
                            Height = height,
                            Plane = plane,
                            Depth = Math.Abs(hostNormal.DotProduct(vector)),
                            MinSizeInMm = Convert.ToInt32(minSize * footToMm),
                            SectionBox = sectionBox,
                        };
                        model.SetSizeDescription();
                        yield return model;
                    }
                }
            }
        }

        #endregion


        #region Validate Intersection And Verify Section Size

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


        private Plane CreatePlaneByNormalAndCentroid(Document doc, in XYZ normal, in XYZ centroid)
        {
            using (Transaction trx = new(doc, "CreatePlane"))
            {
                TransactionStatus status = trx.Start();
                try
                {
                    plane = Plane.CreateByNormalAndOrigin(normal, centroid);
                    status = trx.Commit();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    plane = null;
                }
            }
            return plane;
        }


        public void GetSectionSize(BoundingBoxUV size, ref XYZ normal, out double width, out double height)
        {
            width = 0;
            height = 0;
            if (size != null)
            {
                normal = normal.ConvertToPositive();
                if (normal.IsAlmostEqualTo(XYZ.BasisX, 0.5))
                {
                    width = Math.Round(size.Max.U - size.Min.U, 5);
                    height = Math.Round(size.Max.V - size.Min.V, 5);
                }
                if (normal.IsAlmostEqualTo(XYZ.BasisY, 0.5))
                {
                    width = Math.Round(size.Max.V - size.Min.V, 5);
                    height = Math.Round(size.Max.U - size.Min.U, 5);
                }
            }
        }


        #endregion


        public void VerifyOpenningSize(Document doc, in ElementModel model)
        {
            double offset = Convert.ToDouble(Properties.Settings.Default.CutOffsetInMm / footToMm);

            Plane plane = model.Plane;

            XYZ origin = model.Plane.Origin;

            BoundingBoxUV bbox = model.SectionBox;

            XYZ minPt = origin + bbox.Min.U * plane.XVec + bbox.Min.V * plane.YVec;
            XYZ maxPt = origin + bbox.Max.U * plane.XVec + bbox.Max.V * plane.YVec;

            XYZ pt0 = new XYZ(minPt.X, minPt.Y, minPt.Z);
            XYZ pt1 = new XYZ(maxPt.X, minPt.Y, minPt.Z);
            XYZ pt2 = new XYZ(maxPt.X, maxPt.Y, maxPt.Z);
            XYZ pt3 = new XYZ(minPt.X, maxPt.Y, maxPt.Z);


            IList<CurveLoop> curveloops = new List<CurveLoop>();

            //Solid solid = loops.CreateExtrusionGeometry(model.Normal, model.Depth, offset);
            //using Transaction trans = new(doc, "Create opening");
            //TransactionStatus status = trans.Start();
            //if (status == TransactionStatus.Started)
            //{
            //    solid.CreateDirectShape(doc);
            //    status = trans.Commit();
            //}
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
                    XYZ origin = model.Plane.Origin;
                    if (instanse is Wall wall && wall.IsValidObject)
                    {
                        opening = doc.Create.NewFamilyInstance(origin, wallOpenning, model.HostLevel, StructuralType.NonStructural);
                    }
                    if (instanse is RoofBase roof && roof.IsValidObject)
                    {
                        opening = doc.Create.NewFamilyInstance(origin, floorOpenning, model.HostLevel, StructuralType.NonStructural);
                    }
                    if (instanse is Floor floor && floor.IsValidObject)
                    {
                        opening = doc.Create.NewFamilyInstance(origin, floorOpenning, model.HostLevel, StructuralType.NonStructural);
                    }
                    if (opening != null)
                    {
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


        public static CurveArray ConvertLoopToArray(CurveLoop loop)
        {
            CurveArray a = new();
            foreach (Curve c in loop)
            {
                a.Append(c);
            }
            return a;
        }


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
            searchDocument?.Dispose();
            searchTransform?.Dispose();
        }
    }
}
