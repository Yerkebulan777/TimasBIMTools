using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Windows;

namespace RevitTimasBIMTools.Services
{

    [SuppressUnmanagedCodeSecurity]
    public static class WindowExtesion
    {
        public static System.Windows.Point GetRevitWindowLocationPoint(this UIApplication uiapp, double offset = 3)
        {
            System.Windows.Point point = new();
            IntPtr revitHandle = uiapp.MainWindowHandle;
            if (revitHandle != IntPtr.Zero)
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                IList<UIView> uiViewsWithActiveView = uidoc.GetOpenUIViews();
                UIView activeUIView = uiViewsWithActiveView.FirstOrDefault();
                Autodesk.Revit.DB.Rectangle rectParent = activeUIView.GetWindowRectangle();

                double screenWidth = SystemParameters.FullPrimaryScreenWidth;
                double screenHeight = SystemParameters.FullPrimaryScreenHeight;

                int parentWidth = rectParent.Right - rectParent.Left;
                int parentHeight = rectParent.Bottom - rectParent.Top;

                int centreParentX = Convert.ToInt32(screenWidth / 2) - (parentWidth / 2);
                int centreParentY = Convert.ToInt32(screenHeight / 2) - (parentHeight / 2);

                point.X = centreParentX + (parentWidth / offset);
                point.Y = centreParentY + (parentHeight / offset);
            }
            return point;
        }
    }
}
