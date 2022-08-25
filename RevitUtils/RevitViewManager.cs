using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitViewManager
    {
        public static View3D CreateNew3DView(Document doc, string viewName)
        {
            View3D view = null;
            UIDocument uidoc = new UIDocument(doc);
            ViewFamilyType vft3d = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(q => q.ViewFamily == ViewFamily.ThreeDimensional);
            using (Transaction t = new Transaction(doc, "CreateNew3DView"))
            {
                try
                {
                    _ = t.Start();
                    view = View3D.CreateIsometric(doc, vft3d.Id);
                    _ = view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                    _ = view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(6);
                    view.Name = viewName;
                    _ = t.Commit();
                }
                catch (System.Exception exc)
                {
                    _ = t.RollBack();
                    RevitLogger.Error(string.Format("Error create 3Dview {0}", exc.Message));
                }
                finally
                {
                    vft3d.Dispose();
                }
            }
            return view;
        }

        /// <summary>
        /// Retrieve a suitable 3D view3d from document. 
        /// </summary>
        public static View3D Get3dView(Document doc, string viewName = "Isometric3DView")
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            foreach (View3D v in collector)
            {
                if (!v.IsTemplate && v.Name.Equals(viewName))
                {
                    return v;
                }
            }
            return CreateNew3DView(doc, viewName);
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
            BoundingBoxXYZ bbox = elem.get_BoundingBox(view3d);
            if (bbox != null && bbox.Enabled)
            {
                uidoc.RequestViewChange(view3d);
                using (Transaction t = new Transaction(uidoc.Document, "GetSectionBoxIn3DView"))
                {
                    if (TransactionStatus.Started == t.Start())
                    {
                        view3d.SetSectionBox(bbox);
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
                if (view3d is Autodesk.Revit.DB.View view)
                {
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
                            view.IsolateElementTemporary(elem.Id);
                            if (TransactionStatus.Committed == t.Commit())
                            {
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
}
