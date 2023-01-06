using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SmartBIMTools.RevitUtils
{
    public static class WindowExtesion
    {
        public static Tuple<int, int> SetActiveViewLocation(this UIApplication uiapp, Window window, int offset = 25)
        {
            Tuple<int, int> point = null;
            IntPtr revitHandle = uiapp.MainWindowHandle;
            if (revitHandle != IntPtr.Zero)
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                IList<UIView> uiViewsWithActiveView = uidoc.GetOpenUIViews();
                UIView activeUIView = uiViewsWithActiveView.FirstOrDefault();

                Rectangle viewRect = activeUIView.GetWindowRectangle();

                int ptX = Convert.ToInt16(viewRect.Right - (window.Width + offset));
                int ptY = Convert.ToInt16(viewRect.Bottom - (window.Height + offset));

                point = Tuple.Create(ptX, ptY);
            }
            return point;
        }
    }
}
