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

        private const double epsilon = 0.0005;
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

        private Guid widthGuid = Properties.Settings.Default.WidthMarkGuid;
        private Guid heightGuid = Properties.Settings.Default.HeightMarkGuid;
        private Guid elevatGuid = Properties.Settings.Default.ElevatMarkGuid;
        private double minSideSize = Math.Round((Properties.Settings.Default.MinSideSizeInMm / footToMm) - epsilon, 5);
        private double minDepthSize = Math.Round((Properties.Settings.Default.MinDepthSizeInMm / footToMm) - epsilon, 5);

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
        private double height = 0;
        private double width = 0;
        private double depth = 0;

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
            Properties.Settings.Default.Upgrade();
            Transform global = document.Transform;
            widthGuid = Properties.Settings.Default.WidthMarkGuid;
            heightGuid = Properties.Settings.Default.HeightMarkGuid;
            elevatGuid = Properties.Settings.Default.ElevatMarkGuid;
            IList<ElementModel> output = new List<ElementModel>(50);
            minSideSize = Math.Round((Properties.Settings.Default.MinSideSizeInMm / footToMm) - epsilon, 5);
            minDepthSize = Math.Round((Properties.Settings.Default.MinDepthSizeInMm / footToMm) - epsilon, 5);
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
            hostNormal = host.GetHostNormal();
            hostBbox = host.get_BoundingBox(null);
            hostSolid = host.GetSolidByVolume(identity, options);
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(hostBbox.GetOutLine());
            LogicalAndFilter intersectFilter = new(bboxFilter, new ElementIntersectsSolidFilter(hostSolid));
            collector = new FilteredElementCollector(doc).OfCategoryId(category.Id).WherePasses(intersectFilter);

            foreach (Element elem in collector)
            {
                centroid = elem.GetMiddlePointByBoundingBox(out elemBbox);
                if (IsIntersectionValid(elem, hostSolid, hostNormal, centroid, out vector, out depth))
                {
                    ISet<XYZ> points = hostSolid.GetIntersectionPoints(elem, global, options, ref centroid);

                    plane = CreatePlaneByNormalAndCentroid(doc, hostNormal, centroid);

                    BoundingBoxUV sectionBox = plane.ProjectPointsOnPlane(points);

                    GetSectionSize(sectionBox, ref hostNormal, out width, out height);

                    double minSize = Math.Min(width, height);
                    if (minSideSize < minSize)
                    {
                        ElementModel model = new(elem, host)
                        {
                            Width = width,
                            Depth = depth,
                            Height = height,
                            SectionPlane = plane,
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

        private bool IsIntersectionValid(Element elem, in Solid solid, in XYZ normal, in XYZ centroid, out XYZ vector, out double depth)
        {
            depth = 0;
            vector = XYZ.Zero;
            Line interLine = null;
            if (elem.Location is LocationCurve curve)
            {
                interLine = curve.Curve as Line;
                if (normal.IsAlmostEqualTo(interLine.Direction, threshold))
                {
                    vector = interLine.Direction.Normalize();
                }
            }
            else if (elem is FamilyInstance instance)
            {
                Transform transform = instance.GetTransform();

                if (normal.IsAlmostEqualTo(transform.BasisX, threshold))
                {
                    vector = transform.BasisX.Normalize();
                    interLine = CreateLine(vector, centroid);
                }

                if (normal.IsAlmostEqualTo(transform.BasisY, threshold))
                {
                    vector = transform.BasisY.Normalize();
                    interLine = CreateLine(vector, centroid);
                }
            }

            if (solid != null && interLine != null)
            {
                SolidCurveIntersection curves = solid.IntersectWithCurve(interLine, intersectOptions);
                if (curves != null && 0 < curves.SegmentCount)
                {
                    interLine = curves.GetCurveSegment(0) as Line;
                    vector = interLine.GetEndPoint(1) - interLine.GetEndPoint(0);
                    depth = Math.Round(Math.Abs(normal.DotProduct(vector)), 5);
                }
            }

            return minDepthSize < depth;
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
            XYZ normal = model.SectionPlane.Normal;
            double offset = Convert.ToDouble(Properties.Settings.Default.CutOffsetInMm / footToMm);
            IList<CurveLoop> profile = model.GetSectionProfileWithOffset(offset);
            Solid solid = profile.CreateExtrusionGeometry(normal, model.Depth);
            using Transaction trans = new(doc, "Create opening");
            TransactionStatus status = trans.Start();
            if (status == TransactionStatus.Started)
            {
                solid.CreateDirectShape(doc);
                status = trans.Commit();
            }
        }


        public void CreateOpening(Document doc, ElementModel model)
        {
            using Transaction trx = new(doc);
            if (!model.IsValidModel()) { return; }
            TransactionStatus status = trx.Start("Create opening");
            try
            {
                FamilyInstance opening = null;
                XYZ origin = model.SectionPlane.Origin;
                Level level = doc.GetElement(model.Host.LevelId) as Level;
                if (status == TransactionStatus.Started)
                {
                    if (model.HostCategoryIntId.Equals(-2000011))
                    {
                        FamilySymbol symbol = GetOpeningFamilySymbol(doc, Properties.Settings.Default.WallOpeningSymbolId);
                        opening = doc.Create.NewFamilyInstance(origin, symbol, model.Host, level, StructuralType.NonStructural);
                    }
                    else if (model.HostCategoryIntId.Equals(-2000032) || model.HostCategoryIntId.Equals(-2000035))
                    {
                        FamilySymbol symbol = GetOpeningFamilySymbol(doc, Properties.Settings.Default.FloorOpeningSymbolId);
                        opening = doc.Create.NewFamilyInstance(origin, symbol, model.Host, level, StructuralType.NonStructural);
                    }
                    if (opening != null && opening.IsValidObject)
                    {
                        //double elevLevel = level.ProjectElevation;
                        //var prm = SharedParameterElement.Lookup(doc, elevatGuid);
                        //double elevMark = opening.get_Parameter(elevatGuid).AsDouble();
                        Parameter elevatParam = opening.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                        bool elevatBolean = opening.get_Parameter(elevatGuid).Set(elevatParam.AsDouble());
                        if (opening.get_Parameter(heightGuid).Set(model.Height))
                        {
                            if (opening.get_Parameter(widthGuid).Set(model.Width))
                            {
                                if (elevatBolean && elevatParam.Set(0))
                                {
                                    status = trx.Commit();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
            finally
            {
                if (!trx.HasEnded())
                {
                    status = trx.RollBack();
                }
            }
        }


        private FamilySymbol GetOpeningFamilySymbol(Document doc, string uniqueId)
        {
            FamilySymbol familySymbol = null;
            if (!string.IsNullOrEmpty(uniqueId))
            {
                if (doc.GetElement(uniqueId) is FamilySymbol symbol)
                {
                    familySymbol = symbol;
                }
            }
            return familySymbol;
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
        //    Solid interLine = BooleanOperationsUtils.ExecuteBooleanOperation(solidA, solidB, BooleanOperationsType.Intersect);
        //    return interLine.Volume > 0;
        //}



        //private double GetLengthValueBySimilarParameterName(Instance floor, string paramName)
        //{
        //    double value = invalidInt;
        //    int minDistance = int.MaxValue;
        //    char[] delimiters = new[] { ' ', '_', '-' };
        //    foreach (Parameter param in floor.GetOrderedParameters())
        //    {
        //        Definition highMark = param.Definition;
        //        if (param.HasValue && highMark.ParameterType == lenParamType)
        //        {
        //            string name = highMark.Name;
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
        //        using Transaction trx = new(doc);
        //        _ = trx.Start("Create Temporary Sketch SectionPlane");
        //        try
        //        {
        //            SketchPlane sketchPlan = SketchPlane.Create(doc, reference);
        //            if (null != sketchPlan)
        //            {
        //                SectionPlane plan = sketchPlan.GetPlane();
        //                vector = plan.Normal;
        //                origin = plan.Origin;
        //                flag = true;
        //            }
        //        }
        //        finally
        //        {
        //            _ = trx.RollBack();
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
