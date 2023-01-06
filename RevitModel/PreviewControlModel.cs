using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SmartBIMTools.RevitUtils;
using SmartBIMTools.Views;
using System;
using System.Windows;

namespace SmartBIMTools.RevitModel
{
    public sealed class PreviewControlModel
    {
        private readonly PreviewDialogBox window;
        private PreviewControl previewControl { get; set; }
        public PreviewControlModel(PreviewDialogBox frame)
        {
            window = frame;
        }


        public void ShowPreviewControl(UIApplication uiapp, View3D view3d)
        {

            Document doc = uiapp.ActiveUIDocument.Document;
            Tuple<int, int> point = uiapp.SetActiveViewLocation(window);
            try
            {
                previewControl = new PreviewControl(doc, view3d.Id);
                if (0 > window.GridControl.Children.Add(previewControl))
                {
                    previewControl.Loaded += PreviewControlLoad;
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
                previewControl.Loaded -= PreviewControlLoad;
                previewControl.UIView.ZoomToFit();
            }
        }
    }
}
