using Autodesk.Revit.UI;
using SmartBIMTools.Services;
using SmartBIMTools.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;


namespace SmartBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для PreviewDialogBox.xaml
    /// </summary>
    public partial class PreviewDialogBox : Window
    {
        private readonly CutHoleDataViewModel DataContextHandler;
        private static readonly ExternalEvent externalEvent = CutHoleDataViewModel.RevitExternalEvent;
        public PreviewDialogBox(CutHoleDataViewModel viewModel)
        {
            InitializeComponent();
            Loaded += DialogBox_Loaded;
            WindowStyle = WindowStyle.None;
            DataContext = DataContextHandler = viewModel;
            DataContextHandler = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }


        [STAThread]
        private void DialogBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!DataContextHandler.IsStarted)
            {
                try
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        if (ExternalEventRequest.Accepted == externalEvent.Raise())
                        {
                            DataContextHandler.DialogResult = null;
                        }
                    });
                }
                catch (Exception ex)
                {
                    SBTLogger.Error(ex.Message);
                }
            }
        }


        private void ApplyCmd_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                DataContextHandler.DialogResult = true;
                Close();
            });
        }


        private void CancelCmd_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                DataContextHandler.DialogResult = false;
                Close();
            });
        }

    }
}
