using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.DependencyInjection;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = Autodesk.Revit.DB.Color;



namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitViewManager
    {

        #region GetCreate3dView
        public static View3D CreateNew3DView(UIDocument uidoc, string viewName)
        {
            bool flag = false;
            View3D view3d = null;
            Document doc = uidoc.Document;
            ViewFamilyType vft = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .FirstOrDefault(q => q.ViewFamily == ViewFamily.ThreeDimensional);
            using (Transaction t = new(doc, "CreateNew3DView"))
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
                        vft.Dispose();
                    }
                }
            }
            return view3d;
        }


        public static View3D Get3dView(UIDocument uidoc, string viewName = "Isometric3DView")
        {
            Document doc = uidoc.Document;
            foreach (View3D view3d in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
            {
                if (!view3d.IsTemplate && view3d.Name.Equals(viewName))
                {
                    DisplayStyle style = DisplayStyle.Realistic;
                    ViewDetailLevel level = ViewDetailLevel.Fine;
                    ViewDiscipline discipline = ViewDiscipline.Coordination;
                    SetViewSettings(doc, view3d, discipline, style, level);
                    return view3d;
                }
            }
            return CreateNew3DView(uidoc, viewName);
        }

        #endregion


        #region SetView3DSettings

        public static void SetViewSettings(Document doc, View view, ViewDiscipline discipline, DisplayStyle style, ViewDetailLevel level)
        {
            using Transaction t = new(doc);
            TransactionStatus status = t.Start("SetViewSettings");
            if (status == TransactionStatus.Started)
            {
                try
                {
                    if (view is View3D view3D)
                    {
                        view3D.IsSectionBoxActive = false;
                    }
                    view.ViewTemplateId = ElementId.InvalidElementId;
                    view.Discipline = discipline;
                    view.DisplayStyle = style;
                    view.DetailLevel = level;
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
        public static void ShowElement(UIDocument uidoc, Element elem)
        {
            uidoc.Selection.SetElementIds(new List<ElementId> { elem.Id });
            uidoc.ShowElements(elem);
        }

        #endregion


        #region ShowView
        public static void ShowView(UIDocument uidoc, in View view)
        {
            if (view is not null)
            {
                DisplayStyle style = DisplayStyle.Realistic;
                ViewDetailLevel level = ViewDetailLevel.Fine;
                ViewDiscipline discipline = ViewDiscipline.Coordination;
                SetViewSettings(uidoc.Document, view, discipline, style, level);
                foreach (UIView uv in uidoc.GetOpenUIViews())
                {
                    if (uv.ViewId.Equals(view.Id))
                    {
                        uidoc.RequestViewChange(view);
                        uidoc.RefreshActiveView();
                        uv.ZoomToFit();
                        break;
                    }
                }
            }
        }

        #endregion


        #region CreatePlan
        public static ViewPlan CreatePlan(Document doc, Level level)
        {
            using Transaction tx = new(doc);
            TransactionStatus status = tx.Start("CreateFloorPlan");
            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(x => ViewFamily.AreaPlan == x.ViewFamily);
            ViewPlan floorPlan = ViewPlan.Create(doc, vft.Id, level.Id);
            floorPlan.Discipline = ViewDiscipline.Coordination;
            floorPlan.DisplayStyle = DisplayStyle.Realistic;
            floorPlan.DetailLevel = ViewDetailLevel.Fine;
            status = tx.Commit();
            return floorPlan;
        }
        #endregion


        #region SetCustomSectionBox
        public static bool SetCustomSectionBox(UIDocument uidoc, XYZ centroid, View3D view3d)
        {
            bool result = false;
            uidoc.RequestViewChange(view3d);
            if (uidoc.ActiveView.Id.Equals(view3d.Id))
            {
                using Transaction tx = new(uidoc.Document);
                BoundingBoxXYZ bbox = GetBoundingBox(centroid);
                TransactionStatus status = tx.Start("SetSectionBox");
                if (status == TransactionStatus.Started)
                {
                    ZoomElementInView(uidoc, view3d, bbox);
                    view3d.SetSectionBox(bbox);
                    status = tx.Commit();
                    result = true;
                }
                uidoc.RefreshActiveView();
            }
            return result;
        }


        private static BoundingBoxXYZ GetBoundingBox(XYZ centroid, double factor = 3)
        {
            BoundingBoxXYZ bbox = new();
            XYZ vector = new(factor, factor, factor);
            bbox.Min = centroid - vector;
            bbox.Max = centroid + vector;
            return bbox;
        }


        private static void ZoomElementInView(UIDocument uidoc, View3D view3d, BoundingBoxXYZ box)
        {
            UIView uiview = uidoc.GetOpenUIViews().Cast<UIView>().FirstOrDefault(v => v.ViewId.Equals(view3d.Id));
            if (uiview != null)
            {
                try
                {
                    uiview.ZoomAndCenterRectangle(box.Min, box.Max);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
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

    }
}