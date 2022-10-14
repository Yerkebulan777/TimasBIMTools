using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutVoidDockPaneView.xaml </summary>
    public partial class CutVoidDockPaneView : Page, IDockablePaneProvider, IDisposable
    {
        private readonly Mutex mutex = new();
        public bool Disposed { get; internal set; } = false;
        private ExternalEvent externalEvent { get; set; } = null;
        private readonly CutVoidDataViewModel DataContextHandler;
        private readonly string documentId = Properties.Settings.Default.ActiveDocumentUniqueId;


        public CutVoidDockPaneView(CutVoidDataViewModel handler)
        {
            handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Logger.ThreadProcessLog("Process => " + nameof(CutVoidDockPaneView));

            InitializeComponent();
            handler.DockPanelView = this;
            DataContext = DataContextHandler = handler;
                      
        }


        [STAThread]
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }


        [STAThread]
        public void RaiseHandler()
        {
            DataContextHandler.Completed += OnContextHandlerCompleted;
            externalEvent = ExternalEvent.Create(DataContextHandler);
            if (ExternalEventRequest.Accepted != externalEvent.Raise())
            {
                Logger.Warning("External event request not accepted!!!");
                Logger.ThreadProcessLog("Process => " + nameof(RaiseHandler));
            }
        }


        [STAThread]
        private void OnContextHandlerCompleted(object sender, BaseCompletedEventArgs args)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Disposed = false;
                CommandManager.InvalidateRequerySuggested();
                DataContextHandler.TaskContext = TaskScheduler.FromCurrentSynchronizationContext();
                DataContextHandler.SyncContext = SynchronizationContext.Current;
                if (SynchronizationContext.Current == args.SyncContext)
                {
                    Logger.ThreadProcessLog("Process => " + nameof(OnContextHandlerCompleted));
                }
            }, DispatcherPriority.Background);
        }


        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                RevitTask.RunAsync(app =>
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
                }).Dispose();
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        DataContextHandler.Dispose();
                        Properties.Settings.Default.Reset();
                        DataContextHandler.IsStarted = false;
                        DataContextHandler.IsDataEnabled = false;
                        DataContextHandler.IsOptionEnabled = false;
                    }, DispatcherPriority.Background);
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                    // TODO: освободить управляемое состояние (управляемые объекты)
                }
                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                Disposed = true;
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //GC.SuppressFinalize(this);
        }

    }
}
