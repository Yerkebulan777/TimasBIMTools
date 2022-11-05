using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace RevitTimasBIMTools.Services
{
    public static class WindowExtesion
    {
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        public static Tuple<int, int> SetActiveViewLocation(this UIApplication uiapp, Window window, int offset = 150)
        {
            Tuple<int, int> point = null;
            IntPtr revitHandle = uiapp.MainWindowHandle;
            if (revitHandle != IntPtr.Zero)
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                //var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
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
