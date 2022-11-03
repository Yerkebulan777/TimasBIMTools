using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = Autodesk.Revit.DB.Color;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitViewManager
    {
        //ContentControl content = new PreviewControl(document, view3d.Id);

        #region Get3dView
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


        /// <summary>
        /// Retrieve a suitable 3D view3d from document. 
        /// </summary>
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
                    return SetView3DSettings(doc, view3d, discipline, style, level);
                }
            }
            return CreateNew3DView(uidoc, viewName);
        }


        public static View3D SetView3DSettings(Document doc, View3D view, ViewDiscipline discipline, DisplayStyle style, ViewDetailLevel level)
        {
            using (Transaction t = new(doc, "SetView3DSettings"))
            {
                TransactionStatus status = t.Start();
                if (status == TransactionStatus.Started)
                {
                    try
                    {
                        view.ViewTemplateId = ElementId.InvalidElementId;
                        view.IsSectionBoxActive = false;
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
            return view;
        }
        #endregion


        #region ShowElement
        public static void ShowElement(UIDocument uidoc, Element elem)
        {
            uidoc.Selection.SetElementIds(new List<ElementId> { elem.Id });
            uidoc.ShowElements(elem);
        }

        #endregion


        #region Show3DView
        public static void Show3DView(UIDocument uidoc, View3D view3d)
        {
            if (view3d is not null and View view)
            {
                uidoc.RequestViewChange(view3d);
                using Transaction t = new(uidoc.Document);
                TransactionStatus status = t.Start("Get3DView");
                if (status == TransactionStatus.Started)
                {
                    view3d.ViewTemplateId = ElementId.InvalidElementId;

                    view.Discipline = ViewDiscipline.Mechanical;
                    view.DisplayStyle = DisplayStyle.Realistic;
                    view.DetailLevel = ViewDetailLevel.Fine;

                    status = t.Commit();
                }
                uidoc.RefreshActiveView();
            }
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


        public static BoundingBoxXYZ GetBoundingBox(XYZ centroid, double factor = 3)
        {
            BoundingBoxXYZ bbox = new();
            XYZ vector = new(factor, factor, factor);
            bbox.Min = centroid - vector;
            bbox.Max = centroid + vector;
            return bbox;
        }


        public static void ZoomElementInView(UIDocument uidoc, View3D view3d, BoundingBoxXYZ box)
        {
            UIView uiview = uidoc.GetOpenUIViews().Cast<UIView>().FirstOrDefault(v => v.ViewId.Equals(view3d.Id));
            if (uiview != null)
            {
                try
                {
                    uiview.ZoomAndCenterRectangle(box.Min, box.Max);
                    uiview.Zoom(0.85);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            }
        }


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


        public static void SetCustomColorInView(UIDocument uidoc, View3D view, ElementId solidFillId, Element elem, byte blue = 128, byte red = 128, byte green = 128)
        {
            Color color = new(red, green, blue);
            OverrideGraphicSettings graphics = new();
            if (!view.AreGraphicsOverridesAllowed())
            {
                Logger.Error($"Graphic overrides are not alowed for the '{view.Name}' view3d");
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
                    Logger.Error(exc.Message);
                    if (!tx.HasEnded())
                    {
                        status = tx.RollBack();
                    }
                }
            }
        }


        public static void SetCategoryTransparency(Document doc, View3D view, Category category, int transparency = 0, bool halftone = false)
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

            #endregion

        }
    }
}