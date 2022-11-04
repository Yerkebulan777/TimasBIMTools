using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.Services
{
    public static class WindowExtesion
    {
        public static Tuple<int, int> SetActiveViewLocation(this UIApplication uiapp, int offset = 150)
        {
            Tuple<int, int> point = null;
            IntPtr revitHandle = uiapp.MainWindowHandle;
            if (revitHandle != IntPtr.Zero)
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                IList<UIView> uiViewsWithActiveView = uidoc.GetOpenUIViews();
                UIView activeUIView = uiViewsWithActiveView.FirstOrDefault();
                //Autodesk.Revit.DB.Rectangle windowRect = uiapp.MainWindowExtents;
                //Autodesk.Revit.DB.Rectangle drawingRect = uiapp.DrawingAreaExtents;

                Rectangle viewRect = activeUIView.GetWindowRectangle();

                int viewXLen = viewRect.Right - viewRect.Left;
                int viewYLen = viewRect.Bottom - viewRect.Top;

                double diagonal = Math.Sqrt(Math.Pow(viewXLen, 2) + Math.Pow(viewYLen, 2));
                double scale = Math.Round(1 / (diagonal / offset), 5);

                int ptX = Convert.ToInt16(viewRect.Right - (viewXLen * scale));
                int ptY = Convert.ToInt16(viewRect.Bottom - (viewYLen * scale));

                point = Tuple.Create(ptX, ptY);
            }
            return point;
        }
    }
}
