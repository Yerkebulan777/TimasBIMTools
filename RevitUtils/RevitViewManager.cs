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
            uidoc.RequestViewChange(view3d);
            uidoc.RefreshActiveView();
        }

        #endregion


        #region SetCustomSectionBox
        public static View3D SetCustomSectionBox(UIDocument uidoc, Element elem, View3D view3d)
        {
            uidoc.ActiveView = view3d;
            uidoc.Selection.SetElementIds(new List<ElementId> { elem.Id });
            cmdId = RevitCommandId.LookupPostableCommandId(PostableCommand.SelectionBox);
            bindedCmdId = uidoc.Application.CreateAddInCommandBinding(cmdId);
            bindedCmdId.Executed += BindedSectionBoxCmdId_Executed;
            uidoc.Application.PostCommand(cmdId);
            return view3d;
        }


        private static void BindedSectionBoxCmdId_Executed(object sender, Autodesk.Revit.UI.Events.ExecutedEventArgs e)
        {
            bindedCmdId.Executed -= BindedSectionBoxCmdId_Executed;
            UIApplication uiapp = sender as UIApplication;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc.ActiveView is View3D view3d)
            {
                Document doc = uiapp.ActiveUIDocument.Document;
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                Element elem = doc.GetElement(selectedIds.Cast<ElementId>().FirstOrDefault());
                using (Transaction t = new Transaction(uidoc.Document, "SetCustomSectionBox"))
                {
                    BoundingBoxXYZ bbox = GetBoundingBox(elem, view3d);
                    if (TransactionStatus.Started == t.Start())
                    {
                        view3d.SetSectionBox(bbox);
                    }
                    if (TransactionStatus.Committed == t.Commit())
                    {
                        try
                        {
                            ZoomElementInView(uidoc, view3d, bbox);
                        }
                        finally
                        {
                            uidoc.RefreshActiveView();
                        }

                    }
                }
            }
        }

        public static BoundingBoxXYZ GetBoundingBox(Element elem, View view = null, double factor = 3)
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


        public static void ZoomElementInView(UIDocument uidoc, View3D view3d, BoundingBoxXYZ box)
        {
            uidoc.ActiveView = view3d;
            uidoc.RequestViewChange(view3d);
            UIView uiview = uidoc.GetOpenUIViews().Cast<UIView>().FirstOrDefault(v => v.ViewId.Equals(view3d.Id));
            if (uiview != null)
            {
                uiview.ZoomAndCenterRectangle(box.Min, box.Max);
            }
        }


        public static void SetColorElement(UIDocument uidoc, Element elem, byte blue = 0, byte red = 0, byte green = 0)
        {
            Color color = uidoc.Application.Application.Create.NewColor();
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            if (!color.IsReadOnly)
            {
                color.Blue = blue;
                color.Red = red;
                color.Green = green;
                ogs = ogs.SetProjectionLineColor(color);
                uidoc.ActiveView.SetElementOverrides(elem.Id, ogs);
            }
        }

        #endregion


        #region IsolateElementIn3DView

        public static void IsolateElementIn3DView(UIDocument uidoc, Element elem, View3D view3d)
        {
            cmdId = RevitCommandId.LookupPostableCommandId(PostableCommand.CloseInactiveViews);
            using (Transaction t = new Transaction(uidoc.Document, "IsolateElementIn3DView"))
            {
                View view = view3d;
                uidoc.ActiveView = view3d;
                uidoc.RequestViewChange(view);
                TransactionStatus status = TransactionStatus.Error;
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
                        status = t.Commit();
                    }
                    catch (Exception ex)
                    {
                        if (status != t.RollBack())
                        {
                            status = t.GetStatus();
                            RevitLogger.Error(ex.Message);
                        }
                    }
                    finally
                    {
                        uidoc.Application.PostCommand(cmdId);
                    }
                }
            }
        }

        #endregion


    }
}