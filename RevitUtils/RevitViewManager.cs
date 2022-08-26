using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitViewManager
    {
        public static View3D CreateNew3DView(UIDocument uidoc, string viewName)
        {
            bool flag = false;
            View3D view = null;
            Document doc = uidoc.Document;
            TransactionStatus status = TransactionStatus.Error;
            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(q => q.ViewFamily == ViewFamily.ThreeDimensional);
            using (Transaction t = new Transaction(doc, "CreateNew3DView"))
            {
                try
                {
                    status = t.Start();
                    view = View3D.CreateIsometric(uidoc.Document, vft.Id);
                    flag = view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                    flag = view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(6);
                    view.Name = viewName;
                    status = t.Commit();
                }
                catch (System.Exception ex)
                {
                    status = t.RollBack();
                    RevitLogger.Error($"Error 3Dview {ex.Message}");
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


        public static void ZoomView(UIDocument uidoc, View3D view3d)
        {
            uidoc.ActiveView = view3d;
            UIView uiview = uidoc.GetOpenUIViews().Cast<UIView>().FirstOrDefault(q => q.ViewId == view3d.Id);
            uidoc.RefreshActiveView();
            uiview.ZoomToFit();
        }


        public static BoundingBoxXYZ GetBoundingBox(Element elem, View view = null, double factor = 0.5)
        {
            BoundingBoxXYZ bbox = elem.get_BoundingBox(view);
            if (bbox != null && bbox.Enabled)
            {
                double sizeX = bbox.Max.X - bbox.Min.X;
                double sizeY = bbox.Max.Y - bbox.Min.Y;
                double sizeZ = bbox.Max.Z - bbox.Min.Z;
                double size = new double[] { sizeX, sizeY, sizeZ }.Min();
                XYZ vector = new XYZ(size, size, size) * factor;
                bbox.Min -= vector;
                bbox.Max += vector;
            }
            return bbox;
        }


        public static View3D GetSectionBoxView(UIDocument uidoc, Element elem, View3D view3d)
        {
            ElementId elemId = elem.Id;
            uidoc.ShowElements(elemId);
            uidoc.RequestViewChange(view3d);
            BoundingBoxXYZ bbox = GetBoundingBox(elem, view3d);
            using (Transaction t = new Transaction(uidoc.Document, "GetSectionBoxView"))
            {
                uidoc.ActiveView = view3d;
                if (TransactionStatus.Started == t.Start())
                {
                    view3d.SetSectionBox(bbox);
                }
                if (TransactionStatus.Committed == t.Commit())
                {
                    uidoc.Selection.SetElementIds(new List<ElementId> { elemId });
                    ZoomView(uidoc, view3d);
                }
            }
            return view3d;
        }


        public static void SetColorElement(UIDocument uidoc, Element elem, byte blue = 0, byte red = 0, byte green = 0)
        {
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            Color color = uidoc.Application.Application.Create.NewColor();
            color.Blue = blue;
            color.Red = red;
            color.Green = green;
            ogs = ogs.SetProjectionLineColor(color);
            uidoc.ActiveView.SetElementOverrides(elem.Id, ogs);
        }


        public static void IsolateElementIn3DView(UIDocument uidoc, Element elem, View3D view3d)
        {
            using (Transaction t = new Transaction(uidoc.Document, "IsolateElementIn3DView"))
            {
                View view = view3d;
                uidoc.RequestViewChange(view);
                if (TransactionStatus.Started == t.Start())
                {
                    try
                    {
                        if (elem.IsHidden(view))
                        {
                            view.UnhideElements(new List<ElementId>() { elem.Id });
                        }
                        if (true == view3d.IsSectionBoxActive)
                        {
                            view3d.IsSectionBoxActive = false;
                        }
                        if (view.IsTemporaryHideIsolateActive())
                        {
                            view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                        }
                        if (TransactionStatus.Committed == t.Commit())
                        {
                            view.IsolateElementTemporary(elem.Id);
                            uidoc.ShowElements(elem.Id);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        if (TransactionStatus.RolledBack == t.RollBack())
                        {
                            RevitLogger.Error(ex.Message);
                        }
                    }

                }

            }
        }


    }
}
