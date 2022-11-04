using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.Services
{
    public static class WindowExtesion
    {
        public static Tuple<int, int> GetActiveViewLocation(this UIApplication uiapp, int offset = 300)
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

                //int parentWidth = viewRect.Right - viewRect.Left;
                //int parentHeight = viewRect.Bottom - viewRect.Top;

                Rectangle viewRect = activeUIView.GetWindowRectangle();

                int diagonal = 500;
                int scale = diagonal / offset;
                int ptX = viewRect.Right * 1 / scale;
                int ptY = viewRect.Bottom * 1 / scale;

                point = Tuple.Create(ptX, ptY);

            }

            return point;
        }
    }
}
