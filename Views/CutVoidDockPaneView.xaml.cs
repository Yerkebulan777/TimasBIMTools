using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.CutOpening;
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
        private CutVoidViewExternalHandler handler { get; set; } = null;

        private readonly IServiceProvider provider = SmartToolApp.ServiceProvider;
        private readonly CutVoidDataViewModel DataViewModel = ViewModelLocator.DataViewModel;
        private readonly string documentId = Properties.Settings.Default.ActiveDocumentUniqueId;


        public CutVoidDockPaneView(CutVoidDataViewModel viewModel)
        {
            InitializeComponent();
            viewModel.DockPanelView = this;
            DataContext = DataViewModel = viewModel;
            Logger.ThreadProcessLog("Process => " + nameof(CutVoidDockPaneView));
            viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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
            Logger.ThreadProcessLog("Process => " + nameof(RaiseHandler));
            handler = provider.GetRequiredService<CutVoidViewExternalHandler>();
            handler.Completed += OnContextHandlerCompleted;
            externalEvent = ExternalEvent.Create(handler);
            Disposed = false;

            if (ExternalEventRequest.Accepted != externalEvent.Raise())
            {
                Logger.Warning("External event request not accepted!!!");
            }
        }


        private void OnContextHandlerCompleted(object sender, BaseCompletedEventArgs args)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                DataViewModel.IsStarted = true;
                DataViewModel.IsDataEnabled = false;
                DataViewModel.IsOptionEnabled = false;
                DataViewModel.DocumentModelCollection = args.DocumentModels;
                DataViewModel.ConstructionTypeIds = args.ConstructionTypeIds;
                Logger.ThreadProcessLog("Process => " + nameof(OnContextHandlerCompleted));
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                DataViewModel.TaskContext = TaskScheduler.FromCurrentSynchronizationContext();
                DataViewModel.SyncContext = SynchronizationContext.Current;
                CommandManager.InvalidateRequerySuggested();
            }, DispatcherPriority.DataBind);
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
                        DataViewModel.Dispose();
                        DataViewModel.IsStarted = false;
                        DataViewModel.IsDataEnabled = false;
                        DataViewModel.IsOptionEnabled = false;
                        CommandManager.InvalidateRequerySuggested();
                        Properties.Settings.Default.Reset();
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
