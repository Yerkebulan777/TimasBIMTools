using Autodesk.Revit.UI;
using RevitTimasBIMTools.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolHelper
    {
        public static string RibbonPanelName = "Automation";
        public static string ApplicationName = "SmartBIMTools";
        public static string CutVoidToolName = "Cut Opening Manager";
        public static string CutVoidButtonName = "CutVoidButtonName";
        public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
        public static readonly string AssemblyLocation = Path.GetFullPath(Assembly.Location);
        public static readonly string AssemblyDirectory = Path.GetDirectoryName(AssemblyLocation);
        public static readonly string AssemblyName = Path.GetFileNameWithoutExtension(AssemblyLocation);
        public static readonly string DocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        public static readonly string AppDirPath = Path.Combine(AppDataPath, @"Autodesk\Revit\Addins\2019\RevitTimasBIMTools");
        public static readonly string LocalPath = Path.Combine(DocumentPath, ApplicationName);
        public static readonly string LogPath = Path.Combine(LocalPath, "RevitAsync.log");


        public DockablePaneId CutVoidPaneId { get; } = new(new Guid("{C586E687-A52C-42EE-AC75-CD81EE1E7A9A}"));
        public bool IsActiveStart { get; set; } = false;

        #region IconConvertToImageSource
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);
        internal static ImageSource GetImageSource()
        {
            Bitmap bmp = Resources.baseIcon.ToBitmap();
            IntPtr handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally { _ = DeleteObject(handle); }
        }
        #endregion
    }
}
