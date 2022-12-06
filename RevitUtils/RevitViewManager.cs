using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Color = Autodesk.Revit.DB.Color;
using Level = Autodesk.Revit.DB.Level;
using View = Autodesk.Revit.DB.View;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitViewManager
    {

        #region GetCreate3dView
        public static View3D Create3DView(UIDocument uidoc, string viewName)
        {
            bool flag = false;
            View3D view3d = null;
            Document doc = uidoc.Document;
            ViewFamilyType vft = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .FirstOrDefault(q => q.ViewFamily == ViewFamily.ThreeDimensional);
            using (Transaction t = new(doc, "Create3DView"))
            {
                TransactionStatus status = t.Start();
                if (status == TransactionStatus.Started)
                {
                    try
                    {
                        view3d = View3D.CreateIsometric(uidoc.Document, vft.Id);
                        view3d.Name = viewName;
                        status = t.Commit();
                    }
                    catch (Exception ex)
                    {
                        status = t.RollBack();
                        Logger.Error($"Error 3Dview {ex.Message} flag => {flag}");
                    }
                    finally
                    {
                        ViewDetailLevel detail = ViewDetailLevel.Fine;
                        DisplayStyle style = DisplayStyle.RealisticWithEdges;
                        ViewDiscipline discipline = ViewDiscipline.Mechanical;
                        SetViewSettings(doc, view3d, discipline, style, detail);
                        vft.Dispose();
                    }
                }
            }
            return view3d;
        }


        public static View3D Get3dView(UIDocument uidoc, string viewName = "Preview3DView")
        {
            Document doc = uidoc.Document;
            foreach (View3D view3d in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
            {
                if (!view3d.IsTemplate && view3d.Name.Equals(viewName))
                {
                    ViewDetailLevel detail = ViewDetailLevel.Fine;
                    DisplayStyle style = DisplayStyle.RealisticWithEdges;
                    ViewDiscipline discipline = ViewDiscipline.Mechanical;
                    SetViewSettings(doc, view3d, discipline, style, detail);
                    return view3d;
                }
            }
            return Create3DView(uidoc, viewName);
        }

        #endregion


        #region GetCreatePlanView

        public static ViewPlan GetPlanView(UIDocument uidoc, Level level, string prefix = "Preview")
        {
            if (level == null)
            {
                return null;
            }
            Document doc = uidoc.Document;
            string viewName = prefix + level.Name.Trim();
            foreach (ViewPlan plan in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
            {
                if (!plan.IsTemplate && level.Id.Equals(plan.GenLevel.Id))
                {
                    return plan;
                }
            }
            return CreatePlanView(doc, level, viewName);
        }


        public static ViewPlan CreatePlanView(Document doc, Level level, string name)
        {
            using Transaction tx = new(doc);
            Logger.Log("ViewPlan name: " + name);
            TransactionStatus status = tx.Start("CreateFloorPlan");
            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(x => ViewFamily.FloorPlan == x.ViewFamily);
            ViewPlan floorPlan = ViewPlan.Create(doc, vft.Id, level.Id);
            floorPlan.DisplayStyle = DisplayStyle.ShadingWithEdges;
            floorPlan.Discipline = ViewDiscipline.Coordination;
            floorPlan.DetailLevel = ViewDetailLevel.Fine;
            floorPlan.Name = name;
            status = tx.Commit();
            return floorPlan;
        }

        #endregion


        #region SetViewSettings

        public static void SetViewSettings(Document doc, View view, ViewDiscipline discipline, DisplayStyle style, ViewDetailLevel detail)
        {
            using Transaction t = new(doc);
            TransactionStatus status = t.Start("SetViewSettings");
            if (status == TransactionStatus.Started)
            {
                try
                {
                    view.ViewTemplateId = ElementId.InvalidElementId;
                    view.Discipline = discipline;
                    view.DisplayStyle = style;
                    view.DetailLevel = detail;
                    if (view is View3D view3D)
                    {
                        view3D.IsSectionBoxActive = false;
                    }
                    status = t.Commit();
                }
                catch (Exception ex)
                {
                    status = t.RollBack();
                    Logger.Error(ex.Message);
                }
            }
        }

        #endregion


        #region ShowElement
        public static void ShowModelInPlanView(UIDocument uidoc, in ElementModel model, ViewDiscipline discipline)
        {
            ViewPlan viewPlan = GetPlanView(uidoc, model.HostLevel);
            if (viewPlan != null && model.Instanse.IsValidObject)
            {
                try
                {
                    Document doc = viewPlan.Document;
                    PlanViewRange viewRange = viewPlan.GetViewRange();

                    Element topLevel = doc.GetElement(viewRange.GetLevelId(PlanViewPlane.TopClipPlane));
                    Element botLevel = doc.GetElement(viewRange.GetLevelId(PlanViewPlane.BottomClipPlane));

                    if (topLevel is Level && botLevel is Level && topLevel.Id != botLevel.Id)
                    {
                        double cutPlane = 1350 / 304.8;
                        double offsetPlane = 300 / 304.8;

                        using Transaction trx = new(doc, "SetViewRange");

                        viewRange.SetOffset(PlanViewPlane.CutPlane, cutPlane);
                        viewRange.SetOffset(PlanViewPlane.TopClipPlane, offsetPlane);
                        viewRange.SetOffset(PlanViewPlane.BottomClipPlane, -offsetPlane);
                        viewRange.SetOffset(PlanViewPlane.ViewDepthPlane, -offsetPlane);

                        TransactionStatus status = trx.Start();
                        viewPlan.SetViewRange(viewRange);
                        status = trx.Commit();
                    }
                }
                finally
                {
                    ActivateView(uidoc, viewPlan, discipline);
                    BoundingBoxXYZ bbox = CreateBoundingBox(viewPlan, model.Instanse, model.SectionPlane.Origin);
                    ZoomElementInView(uidoc, viewPlan, bbox);
                    uidoc.RefreshActiveView();
                }
            }
        }


        public static void ShowElements(UIDocument uidoc, IList<ElementId> elems)
        {
            if (elems.Any())
            {
                uidoc.Selection.Dispose();
                uidoc.ShowElements(elems);
            }
        }

        #endregion


        #region ActivateView
        public static void ActivateView(UIDocument uidoc, in View view, ViewDiscipline discipline)
        {
            ElementId activeId = uidoc.ActiveGraphicalView.Id;
            if (view != null && activeId != view.Id)
            {
                uidoc.Selection.Dispose();
                uidoc.RequestViewChange(view);
                ViewDetailLevel detail = ViewDetailLevel.Fine;
                DisplayStyle style = DisplayStyle.ShadingWithEdges;
                SetViewSettings(uidoc.Document, view, discipline, style, detail);
                foreach (UIView uv in uidoc.GetOpenUIViews())
                {
                    ElementId vid = uv.ViewId;
                    if (vid == view.Id)
                    {
                        uv.ZoomToFit();
                    }
                    else if (activeId != vid)
                    {
                        try
                        {
                            uv.Close();
                            uv.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.Message);
                        }
                    }
                }
            }
        }
        #endregion


        #region ZoomElementInView
        private static void ZoomElementInView(UIDocument uidoc, View view, BoundingBoxXYZ box)
        {
            UIView uiview = uidoc.GetOpenUIViews().Cast<UIView>().FirstOrDefault(v => v.ViewId.Equals(view.Id));
            if (box != null && box.Enabled)
            {
                uiview?.ZoomAndCenterRectangle(box.Min, box.Max);
            }
            else
            {
                uiview?.ZoomToFit();
            }
        }
        #endregion


        #region SetCustomSectionBox
        public static bool SetCustomSectionBox(UIDocument uidoc, XYZ centroid, View3D view3d)
        {
            bool result = false;
            if (view3d != null && view3d.IsValidObject)
            {
                using Transaction tx = new(uidoc.Document);
                BoundingBoxXYZ bbox = CreateBoundingBox(centroid);
                TransactionStatus status = tx.Start("SetSectionBox");
                if (status == TransactionStatus.Started)
                {
                    try
                    {
                        ZoomElementInView(uidoc, view3d, bbox);
                        view3d.SetSectionBox(bbox);
                        status = tx.Commit();
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        status = tx.RollBack();
                        Logger.Error(ex.Message);
                    }
                }
            }
            return result;
        }


        private static BoundingBoxXYZ CreateBoundingBox(XYZ centroid, double offset = 3)
        {
            BoundingBoxXYZ bbox = new();
            XYZ vector = new(offset, offset, offset);
            bbox.Min = centroid - vector;
            bbox.Max = centroid + vector;
            bbox.Enabled = true;
            return bbox;
        }


        private static BoundingBoxXYZ CreateBoundingBox(ViewPlan viewPlan, Element element, XYZ centroid, double offset = 9)
        {
            BoundingBoxXYZ bbox = element.get_BoundingBox(viewPlan);
            if (bbox != null && bbox.Enabled)
            {
                bbox.Min = new XYZ(centroid.X - offset, centroid.Y - offset, bbox.Min.Z);
                bbox.Max = new XYZ(centroid.X + offset, centroid.Y + offset, bbox.Max.Z);
            }
            else
            {
                bbox = CreateBoundingBox(centroid, offset);
                bbox.Min = new XYZ(bbox.Min.X, bbox.Min.Y, viewPlan.Origin.Z);
                bbox.Max = new XYZ(bbox.Max.X, bbox.Max.Y, viewPlan.Origin.Z);
            }
            return bbox;
        }

        #endregion


        #region SetCategoryTransparency
        public static void SetCategoryTransparency(Document doc, View3D view, Category category, int transparency = 15, bool halftone = false)
        {
            ElementId catId = category.Id;
            if (view.IsCategoryOverridable(catId))
            {
                OverrideGraphicSettings graphics = new();
                graphics = graphics.SetHalftone(halftone);
                graphics = graphics.SetSurfaceTransparency(transparency);
                using Transaction tx = new(doc, "Override Categories");
                TransactionStatus status = tx.Start();
                try
                {
                    view.SetCategoryOverrides(catId, graphics);
                    status = tx.Commit();
                }
                catch (Exception exc)
                {
                    Logger.Error(exc.Message);
                    if (!tx.HasEnded())
                    {
                        status = tx.RollBack();
                    }
                }
            }

        }

        #endregion


        #region SetCustomColor
        public static ElementId GetSolidFillPatternId(Document doc)
        {
            ElementId solidFillPatternId = null;
            foreach (FillPatternElement fp in new FilteredElementCollector(doc).WherePasses(new ElementClassFilter(typeof(FillPatternElement))))
            {
                FillPattern pattern = fp.GetFillPattern();
                if (pattern.IsSolidFill)
                {
                    solidFillPatternId = fp.Id;
                    break;
                }
            }
            return solidFillPatternId;
        }


        public static void SetCustomColor(UIDocument uidoc, View3D view, ElementId solidFillId, Element elem, Color color = null)
        {
            color ??= new(255, 0, 0);
            OverrideGraphicSettings graphics = new();
            if (!view.AreGraphicsOverridesAllowed())
            {
                Logger.Error($"Graphic overrides are not alowed for the '{view.Name}' View3d");
            }
            else
            {
                graphics = graphics.SetSurfaceForegroundPatternVisible(true);
                graphics = graphics.SetSurfaceBackgroundPatternVisible(true);
                graphics = graphics.SetSurfaceForegroundPatternColor(color);
                graphics = graphics.SetSurfaceBackgroundPatternColor(color);
                graphics = graphics.SetSurfaceForegroundPatternId(solidFillId);
                graphics = graphics.SetSurfaceBackgroundPatternId(solidFillId);

                using Transaction tx = new(uidoc.Document, "Override Color");
                TransactionStatus status = tx.Start();
                try
                {
                    view.SetElementOverrides(elem.Id, graphics);
                    status = tx.Commit();
                }
                catch (Exception exc)
                {
                    if (!tx.HasEnded())
                    {
                        status = tx.RollBack();
                        Logger.Error(exc.Message);
                    }
                }
            }
        }

        #endregion


        public static void CreateViewFilter(Document doc, View view, Element elem, ElementFilter filter)
        {
            string filterName = "Filter" + elem.Name;
            OverrideGraphicSettings ogSettings = new();
            IList<ElementId> categories = CheckFilterableCategoryByElement(elem);
            ParameterFilterElement prmFilter = ParameterFilterElement.Create(doc, filterName, categories, filter);
            ogSettings = ogSettings.SetProjectionLineColor(new Color(255, 0, 0));
            view.SetFilterOverrides(prmFilter.Id, ogSettings);
        }


        public static void ShowFilterableParameters(Document doc, Element elem)
        {
            IList<ElementId> categories = new List<ElementId>() { elem.Category.Id };
            StringBuilder builder = new StringBuilder("FilterableParametrsByElement");
            foreach (ElementId prmId in ParameterFilterUtilities.GetFilterableParametersInCommon(doc, categories))
            {
                _ = builder.AppendLine(LabelUtils.GetLabelFor((BuiltInParameter)prmId.IntegerValue));
            }
            Logger.Info(builder.ToString());
            builder.Clear();
        }


        //ParameterValueProvider provider = new ParameterValueProvider(new ElementId((int)BuiltInParameter.ID_PARAM));
        //FilterElementIdRule rule = new FilterElementIdRule(provider, new FilterNumericEquals(), view.Id);


        private static IList<ElementId> CheckFilterableCategoryByElement(Element elem)
        {
            ICollection<ElementId> catIds = ParameterFilterUtilities.GetAllFilterableCategories();
            IList<ElementId> categories = new List<ElementId>();
            foreach (ElementId catId in catIds)
            {
                if (elem.Category.Id == catId)
                {
                    categories.Add(catId);
                    break;
                }
            }
            return categories;
        }
    }
}