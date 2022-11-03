using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;


namespace RevitTimasBIMTools.Services
{

    [SuppressUnmanagedCodeSecurity]
    public static class NativeWindowMethod
    {
        private const string user32 = "user32.dll";

        [DllImport(user32, SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport(user32, SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref System.Drawing.Rectangle lpRect);

        [DllImport(user32, SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport(user32, SetLastError = true)]
        public static extern void MoveWindow(IntPtr hwnd, int X, int Y, int nWidth, int nHeight, bool rePaint);

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, uint threadId);

        [DllImport(user32, SetLastError = true)]
        public static extern int UnhookWindowsHookEx(IntPtr idHook);

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport(user32, ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport(user32, ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);


        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);


        private static readonly IntPtr HWND_TOP = new(0);
        private static readonly IntPtr HWND_BOTTOM = new(1);
        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new(-2);

        private const uint NOSIZE = 0x0001;
        private const uint NOZORDER = 0x0004;
        private const uint NOACTIVATE = 0x0010;
        private const uint NOOWNERZORDER = 0x0200;
        private const uint NOSENDCHANGING = 0x0400;
        private const uint ASYNCWINDOWPOS = 0x4000;

        public const uint TOPMOST_FLAGS = NOACTIVATE | NOOWNERZORDER | NOSIZE | NOZORDER | NOSENDCHANGING | ASYNCWINDOWPOS;


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
                System.Drawing.Rectangle screen = System.Windows.Forms.Screen.FromHandle(revitHandle).Bounds;

                int widthParent = rectParent.Right - rectParent.Left;
                int heightParent = rectParent.Bottom - rectParent.Top;

                int centreParentX = screen.Left + (screen.Width / 2) - (widthParent / 2);
                int centreParentY = screen.Top + (screen.Height / 2) - (heightParent / 2);

                point.X = centreParentX + (widthParent / offset);
                point.Y = centreParentY + (heightParent / offset);
            }
            return point;
        }
    }
}
