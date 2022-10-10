using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutVoidDockPanelView.xaml </summary>
    public partial class CutVoidDockPanelView : Page, IDockablePaneProvider, IDisposable
    {
        private bool disposedValue = false;
        private readonly Mutex mutex = new();
        private readonly CutVoidDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly string documentId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly TaskScheduler syncContext = TaskScheduler.FromCurrentSynchronizationContext();


        public CutVoidDockPanelView()
        {
            InitializeComponent();
            Loaded += DockPanelView_Loaded;
        }


        [STAThread]
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.PropertiesPalette
            };
        }


        [STAThread]
        private void DockPanelView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.ShutdownStarted += OnShutdownStarted;
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                dataViewModel.Dispose();
                DataContext = dataViewModel;
                dataViewModel.DockPanelView = this;
            });
        }


        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                Task task = RevitTask.RunAsync(app =>
                {
                    Document doc = app.ActiveUIDocument.Document;
                    if (documentId.Equals(doc.ProjectInformation.UniqueId))
                    {
                        if (mutex.WaitOne())
                        {
                            System.Windows.Clipboard.SetText(model.IdInt.ToString());
                            Element elem = doc.GetElement(new ElementId(model.IdInt));
                            RevitViewManager.ShowElement(app.ActiveUIDocument, elem);
                            mutex.ReleaseMutex();
                        }
                    }
                });
            }
        }


        private void OnShutdownStarted(object sender, EventArgs e)
        {
            Dispatcher.ShutdownStarted -= OnShutdownStarted;
            Dispose();
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Task task = Task.Run(Properties.Settings.Default.Reset)
                    .ContinueWith(_ =>
                    {
                        dataViewModel?.Dispose();
                    }, syncContext);
                    // TODO: освободить управляемое состояние (управляемые объекты)
                }
                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(true);
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            GC.SuppressFinalize(this);
        }

    }
}
