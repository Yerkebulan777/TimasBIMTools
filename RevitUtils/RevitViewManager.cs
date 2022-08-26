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


        public static View3D GetSectionBoxView(UIDocument uidoc, Element elem, View3D view3d)
        {
            BoundingBoxXYZ result = new BoundingBoxXYZ();
            BoundingBoxXYZ bbox = elem.get_BoundingBox(view3d);
            double size = (bbox.Max - bbox.Min).GetLength();
            XYZ vector = new XYZ(size, size, size) * 0.25;
            result.Transform = Transform.Identity;
            result.Min = bbox.Min - vector;
            result.Max = bbox.Min + vector;
            if (bbox != null && bbox.Enabled)
            {
                uidoc.RequestViewChange(view3d);
                using (Transaction t = new Transaction(uidoc.Document, "GetSectionBoxIn3DView"))
                {
                    if (TransactionStatus.Started == t.Start())
                    {
                        view3d.SetSectionBox(result);
                    }
                    if (TransactionStatus.Committed == t.Commit())
                    {
                        uidoc.ShowElements(elem.Id);
                    }
                }
            }
            return view3d;
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
