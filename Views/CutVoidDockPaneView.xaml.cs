using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutVoidDockPaneView.xaml </summary>
    public partial class CutVoidDockPaneView : Page, IDockablePaneProvider
    {
        public bool Disposed { get; set; } = false;

        private readonly CutVoidDataViewModel DataContextHandler;

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
            try
            {
                _ = Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    Disposed = false;
                    DataContextHandler.IsStarted = true;
                    DataContextHandler.DockPanelView = this;
                    DataContextHandler.StartHandlerExecute();
                }, DispatcherPriorityAwaiter.ReferenceEquals(DataContextHandler, ActiveDocTitle));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }


        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                if (model.Instanse.IsValidObject)
                {
                    DataContextHandler.GetElementInViewByIntId(model.Instanse.Id);
                }
            }
        }


        public void Dispose()
        {
            if (!Disposed)
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    Disposed = true;
                    DataContextHandler?.Dispose();
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
