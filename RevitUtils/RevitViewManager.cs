using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Color = Autodesk.Revit.DB.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

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
                    SetView3DSettings(doc, view3d, discipline, style, level);
                    return view3d;
                }
            }
            return CreateNew3DView(uidoc, viewName);
        }

        #endregion


        #region SetView3DSettings

        public static void SetView3DSettings(Document doc, View3D view, ViewDiscipline discipline, DisplayStyle style, ViewDetailLevel level)
        {
            using Transaction t = new(doc, "SetView3DSettings");
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
            if (view3d is not null)
            {
                uidoc.RequestViewChange(view3d);
                DisplayStyle style = DisplayStyle.Realistic;
                ViewDetailLevel level = ViewDetailLevel.Fine;
                ViewDiscipline discipline = ViewDiscipline.Coordination;
                SetView3DSettings(uidoc.Document, view3d, discipline, style, level);
                uidoc.RefreshActiveView();
            }
        }

        #endregion




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

        }


        public static Task<bool?> ShowDialogBox(UIDocument uidoc, string promptInfo)
        {
            bool? dialogResult = null;
            Process process = System.Diagnostics.Process.GetCurrentProcess();
            IntPtr revitHandle = process.MainWindowHandle;

            if (revitHandle != IntPtr.Zero)
            {
                IList<UIView> uiViewsWithActiveView = uidoc.GetOpenUIViews();
                UIView activeUIView = uiViewsWithActiveView.FirstOrDefault();
                Rectangle rectParent = activeUIView.GetWindowRectangle();

                System.Drawing.Rectangle screen = Screen.FromHandle(revitHandle).Bounds;

                int widthParent = rectParent.Right - rectParent.Left;
                int heightParent = rectParent.Bottom - rectParent.Top;

                int centreParentX = screen.Left + (screen.Width / 2) - (widthParent / 2);
                int centreParentY = screen.Top + (screen.Height / 2) - (heightParent / 2);

                int pntX = centreParentX + (widthParent / 5);
                int pntY = centreParentY + (heightParent / 5);

                Window window = new()
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Content = new DialogBox(),
                    ClipToBounds = true,
                    Title = promptInfo,
                    Left = pntX,
                    Top = pntY,
                };
                window.Show();
                window.Focus();

                //TaskDialogCommonButtons buttons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel;
                //TaskDialog taskDialog = new("SmartBIMTools")
                //{
                //    Id = "Customer DialogId",
                //    MainContent = promptInfo,
                //    CommonButtons = buttons,
                //    DefaultButton = TaskDialogResult.Ok,
                //};

                //await Task.Delay(1000);
                //TaskDialogResult result = taskDialog.Show();
                //process = Process.GetProcessesByName("SmartBIMTools").FirstOrDefault();
                //IntPtr handle = process.MainWindowHandle;
                //if (handle != IntPtr.Zero)
                //{

                //    NativeWindowMethod.MoveWindow(handle, pntX, pntY, 500, 300, true);
                //}
                //if (TaskDialogResult.Cancel == result)
                //{
                //    dialogResult = false;
                //    taskDialog.Dispose();
                //}
                //else if (TaskDialogResult.Ok == result)
                //{
                //    dialogResult = true;
                //    taskDialog.Dispose();
                //}
            }
            return Task.FromResult(dialogResult);
        }


        //public static void GetRectangleView(UIDocument uidoc)
        //{
        //    View activeView = uidoc.ActiveView;
        //    List<UIView> uiViewsWithActiveView = new();
        //    foreach (UIView uiv in uidoc.GetOpenUIViews())
        //    {
        //        if (uiv.ViewId.IntegerValue == activeView.Id.IntegerValue)
        //        {
        //            uiViewsWithActiveView.Add(uiv);
        //        }
        //    }

        //    UIView ActiveUIView = uiViewsWithActiveView.FirstOrDefault();
        //    if (uiViewsWithActiveView.Count > 1)
        //    {
        //        Process process = System.Diagnostics.Process.GetCurrentProcess();

        //        IntPtr revitHandle = process.MainWindowHandle;
        //        AutomationElement root = AutomationElement.FromHandle(revitHandle);
        //        // find the container control for the open views   				
        //        PropertyCondition workSpaceCondition = new(AutomationElement.ClassNameProperty, "MDIClient");
        //        AutomationElement workspace = root.FindFirst(TreeScope.Descendants, workSpaceCondition);
        //        //find the active window in the workspace == first childwindow
        //        AutomationElement firstviewWindow = workspace.FindFirst(TreeScope.Children, Condition.TrueCondition);
        //        PropertyCondition classCondition = new(AutomationElement.ClassNameProperty, "AfxFrameOrView110u");
        //        AutomationElement viewPane = firstviewWindow.FindFirst(TreeScope.Children, classCondition);

        //        object boundingRectNoDefault = viewPane.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty, true);

        //        //select uiview with identical clientrectangle
        //        System.Windows.Rect boundingRect = (System.Windows.Rect)boundingRectNoDefault;

        //        foreach (UIView uiv in uiViewsWithActiveView)
        //        {
        //            Rectangle rectangle = uiv.GetWindowRectangle();
        //            if (rectangle.Left == boundingRect.Left && rectangle.Top == boundingRect.Top
        //               && rectangle.Right == boundingRect.Right && rectangle.Bottom == boundingRect.Bottom)
        //            {
        //                ActiveUIView = uiv;
        //                break;
        //            }
        //        }
        //    }
        //    if (ActiveUIView == null)
        //    {
        //        return;
        //    }


        //    Rectangle rect = ActiveUIView.GetWindowRectangle();
        //    IList<XYZ> corners = ActiveUIView.GetZoomCorners();
        //    XYZ p = corners[0];
        //    XYZ q = corners[1];

        //    string msg = $"UIView Windows rectangle size: {rect.Left} {rect.Right}  {rect.Top} {rect.Bottom}  and Corners: {p} {q}";

        //    Logger.Info(msg);
        //}

    }
}