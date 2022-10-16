using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutVoidDockPaneView.xaml </summary>
    public partial class CutVoidDockPaneView : Page, IDockablePaneProvider, IDisposable
    {

        public bool Disposed { get; set; } = false;
        private string documentId { get; set; } = Properties.Settings.Default.ActiveDocumentUniqueId;

        private readonly CutVoidDataViewModel DataContextHandler = null;
        public CutVoidDockPaneView(CutVoidDataViewModel viewModel)
        {
            InitializeComponent();
            DataContext = DataContextHandler = viewModel;
            DataContextHandler = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }


        [STAThread]
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.VisibleByDefault = false;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }


        [STAThread]
        public void RaiseEvent()
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Disposed = false;
                DataContextHandler.IsStarted = true;
                DataContextHandler.DockPanelView = this;
            });
        }


        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                DataContextHandler.GetElementInView(model);
            }
        }


        public void Dispose()
        {
            if (!Disposed)
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    Disposed = true;
                    DataContextHandler.Dispose();
                    DataContextHandler.IsStarted = false;
                    DataContextHandler.IsDataEnabled = false;
                    DataContextHandler.IsOptionEnabled = false;
                }, DispatcherPriority.Background);
                Dispatcher.CurrentDispatcher.InvokeShutdown();
                // TODO: освободить управляемое состояние (управляемые объекты)
            }
        }
    }
}
