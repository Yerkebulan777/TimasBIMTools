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
        private static RevitCommandId cmdId { get; set; } = null;
        private static AddInCommandBinding bindedCmdId { get; set; } = null;

        //ContentControl content = new PreviewControl(document, view3d.Id);

        #region Get3dView
        public static View3D CreateNew3DView(UIDocument uidoc, string viewName)
        {
            bool flag = false;
            View3D view = null;
            Document doc = uidoc.Document;
            TransactionStatus status = TransactionStatus.Error;
            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(q => q.ViewFamily == ViewFamily.ThreeDimensional);
            using (Transaction t = new(doc, "CreateNew3DView"))
            {
                try
                {
                    status = t.Start();
                    view = View3D.CreateIsometric(uidoc.Document, vft.Id);
                    flag = view.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE).Set(3);
                    flag = view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                    flag = view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(5);
                    view.Name = viewName;
                    status = t.Commit();
                }
                catch (System.Exception ex)
                {
                    status = t.RollBack();
                    Logger.Error($"Error 3Dview {ex.Message}");
                }
                finally
                {
                    vft.Dispose();
                }
            }
            return view;
        }
        /// <summary>
        /// Retrieve a suitable 3D view3d from document. 
        /// </summary>
        public static View3D Get3dView(UIDocument uidoc, string viewName = "Isometric3DView")
        {
            foreach (View3D v in new FilteredElementCollector(uidoc.Document).OfClass(typeof(View3D)))
            {
                if (!v.IsTemplate && v.Name.Equals(viewName))
                {
                    return v;
                }
            }
            return CreateNew3DView(uidoc, viewName);
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
            if (view3d != null)
            {
                uidoc.RequestViewChange(view3d);
                uidoc.RefreshActiveView();
            }
        }

        #endregion


        #region SetCustomSectionBox
        public static View3D SetCustomSectionBox(UIDocument uidoc, XYZ centroid, View3D view3d)
        {
            uidoc.RequestViewChange(view3d);

            if (uidoc.ActiveView.Id.Equals(view3d.Id))
            {
                BoundingBoxXYZ bbox = GetBoundingBox(centroid);
                using Transaction tx = new(uidoc.Document, "Set Section Box");
                TransactionStatus status = tx.Start();
                ZoomElementInView(uidoc, view3d, bbox);
                view3d.SetSectionBox(bbox);
                uidoc.RefreshActiveView();
                status = tx.Commit();
            }
            return view3d;
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
            Color color = uidoc.Application.Application.Create.NewColor();
            OverrideGraphicSettings graphics = new();
            if (!color.IsReadOnly)
            {
                color.Red = red;
                color.Blue = blue;
                color.Green = green;

                graphics = graphics.SetSurfaceForegroundPatternColor(color);
                graphics = graphics.SetSurfaceForegroundPatternVisible(true);
                graphics = graphics.SetSurfaceForegroundPatternId(solidFillId);

                view.SetElementOverrides(elem.Id, graphics);
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