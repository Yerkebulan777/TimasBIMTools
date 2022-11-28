using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;
using System;
using System.Windows;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class PreviewControlModel
    {
        private readonly PreviewDialogBox window;
        private PreviewControl PreviewControl { get; set; }
        public PreviewControlModel(PreviewDialogBox frame)
        {
            window = frame;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
        }


        public void ShowPreviewControl(UIApplication uiapp, View3D view3d)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            Tuple<int, int> point = uiapp.SetActiveViewLocation(window);
            try
            {
                PreviewControl = new PreviewControl(doc, view3d.Id);
                if (0 > window.GridControl.Children.Add(PreviewControl))
                {
                    PreviewControl.Loaded += PreviewControlLoad;
                }
            }
            finally
            {
                window.ShowInTaskbar = true;
                window.Left = point.Item1;
                window.Top = point.Item2;
                window.Show();
            }
        }


        private void PreviewControlLoad(object sender, RoutedEventArgs e)
        {
            if (window.Activate())
            {
                PreviewControl.Loaded -= PreviewControlLoad;
                PreviewControl.UIView.ZoomToFit();
            }
        }
    }
}
