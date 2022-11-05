using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace RevitTimasBIMTools.Services
{
    public static class WindowExtesion
    {
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        public static Tuple<int, int> SetActiveViewLocation(this UIApplication uiapp, UIElement element, int offset = 150)
        {
            Tuple<int, int> point = null;
            IntPtr revitHandle = uiapp.MainWindowHandle;
            if (revitHandle != IntPtr.Zero)
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                IList<UIView> uiViewsWithActiveView = uidoc.GetOpenUIViews();
                UIView activeUIView = uiViewsWithActiveView.FirstOrDefault();

                Rectangle viewRect = activeUIView.GetWindowRectangle();

                Size size = GetElementPixelSize(element);

                int ptX = Convert.ToInt16(viewRect.Right - (size.Width / 2) - offset);
                int ptY = Convert.ToInt16(viewRect.Bottom - (size.Height / 2) - offset);

                point = Tuple.Create(ptX, ptY);
            }
            return point;
        }


        public static Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            PresentationSource source = PresentationSource.FromVisual(element);
            if (source != null)
            {
                transformToDevice = source.CompositionTarget.TransformToDevice;
            }
            else
            {
                using HwndSource sourceHwnd = new(new HwndSourceParameters());
                transformToDevice = sourceHwnd.CompositionTarget.TransformToDevice;
            }

            if (element.DesiredSize == new Size())
            {
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            return (Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }

    }
}
