using System;
using System.Runtime.InteropServices;

namespace RevitTimasBIMTools.RevitUtils
{
    public class RevitSelectManager
    {
        #region Triggering External Event Execute by Setting Focus
        //Thanks for solution:
        //https://github.com/jeremytammik/RoomEditorApp/tree/master/RoomEditorApp
        //https://thebuildingcoder.typepad.com/blog/2013/12/triggering-immediate-external-event-execute.html
        //https://thebuildingcoder.typepad.com/blog/2016/03/implementing-the-trackchangescloud-external-event.html#5

        /// <summary>
        /// Функция GetForegroundWindow возвращает дескриптор в окно переднего плана
        /// </summary>
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Move the window associated with the passed 
        /// handle to the front.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void SetFocusToRevit()
        {
            IntPtr hRevit = Autodesk.Windows.ComponentManager.ApplicationWindow;
            IntPtr hBefore = GetForegroundWindow();

            if (hBefore != hRevit)
            {
                SetForegroundWindow(hRevit);
                SetForegroundWindow(hBefore);
            }
        }

        #endregion

    }
}
