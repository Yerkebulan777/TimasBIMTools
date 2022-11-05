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
        private readonly Document doc;
        private readonly PreviewDialogBox window;
        private readonly UIApplication application;
        private PreviewControl PreviewControl { get; set; }
        public PreviewControlModel(UIApplication uiapp, PreviewDialogBox frame)
        {
            window = frame;
            application = uiapp;
            doc = application.ActiveUIDocument.Document;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
        }


        public void ShowPreviewControl(View3D view3d)
        {
            Tuple<int, int> point = application.SetActiveViewLocation();
            PreviewControl = new PreviewControl(doc, view3d.Id);
            _ = window.GridControl.Children.Add(PreviewControl);
            PreviewControl.Loaded += PreviewControlLoad;
            window.Left = point.Item1;
            window.Top = point.Item2;
            window.Show();
        }


        private void PreviewControlLoad(object sender, RoutedEventArgs e)
        {
            PreviewControl.Loaded -= PreviewControlLoad;
            PreviewControl.UIView.ZoomToFit();
            window.Activate();
        }
    }
}
