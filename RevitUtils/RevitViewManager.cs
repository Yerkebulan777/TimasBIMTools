using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;

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
                    t.Start();
                    view = View3D.CreateIsometric(doc, vft3d.Id);
                    view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                    view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(6);
                    view.Name = viewName;
                    t.Commit();
                }
                catch (System.Exception exc)
                {
                    t.RollBack();
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


        public static void ZoomView(UIDocument uidoc, View3D view)
        {
            uidoc.ActiveView = view;
            uidoc.RefreshActiveView();
            UIView uiview = uidoc.GetOpenUIViews().Cast<UIView>().FirstOrDefault(q => q.ViewId == view.Id);
            uiview.ZoomToFit();
        }


        public static void IsolateElementIn3DView(UIDocument uidoc, Element elem, View3D view3d)
        {
            using (Transaction t = new Transaction(uidoc.Document, "IsolateElementIn3DView"))
            {
                if (view3d is Autodesk.Revit.DB.View view)
                {

                    uidoc.RequestViewChange(view);
                    List<ElementId> ids = new List<ElementId>();
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
                            t.Commit();
                        }
                        catch (System.Exception exc)
                        {
                            t.RollBack();
                            RevitLogger.Error(exc.Message);
                        }
                        finally
                        {
                            ids.Add(elem.Id);
                            ZoomView(uidoc, view3d);
                            uidoc.ShowElements(ids);
                        }
                    }
                }
            }
        }
    }
}
