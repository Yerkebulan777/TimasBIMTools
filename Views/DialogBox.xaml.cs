using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для DialogBox.xaml
    /// </summary>
    public partial class DialogBox : Window
    {
        private readonly CutVoidDataViewModel DataContextHandler;
        private static readonly ExternalEvent externalEvent = CutVoidDataViewModel.RevitExternalEvent;
        public DialogBox(CutVoidDataViewModel viewModel)
        {
            InitializeComponent();
            Loaded += DialogBox_Loaded;
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
                    Logger.Error(ex.Message);
                }
            }
        }


        private void ApplyCmd_Click(object sender, RoutedEventArgs e)
        {

        }


        private void CancelCmd_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
