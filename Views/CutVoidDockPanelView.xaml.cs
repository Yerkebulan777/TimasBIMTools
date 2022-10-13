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
    /// <summary> Логика взаимодействия для CutVoidDockPanelView.xaml </summary>
    public partial class CutVoidDockPanelView : Page, IDockablePaneProvider, IDisposable
    {
        private readonly Mutex mutex = new();
        public bool Disposed { get; internal set; } = false;
        private ExternalEvent externalEvent { get; set; } = null;
        private CutVoidViewExternalHandler handler { get; set; } = null;

        private readonly IServiceProvider provider = SmartToolApp.ServiceProvider;
        private readonly CutVoidDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly string documentId = Properties.Settings.Default.ActiveDocumentUniqueId;


        public CutVoidDockPanelView()
        {
            InitializeComponent();
            DataContext = dataViewModel;
            dataViewModel.DockPanelView = this;
            Logger.ThreadProcessLog("Process => " + nameof(CutVoidDockPanelView));
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
                dataViewModel.IsStarted = true;
                dataViewModel.IsDataEnabled = false;
                dataViewModel.IsOptionEnabled = false;
                dataViewModel.DocumentModelCollection = args.DocumentModels;
                dataViewModel.ConstructionTypeIds = args.ConstructionTypeIds;
                Logger.ThreadProcessLog("Process => " + nameof(OnContextHandlerCompleted));
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                dataViewModel.TaskContext = TaskScheduler.FromCurrentSynchronizationContext();
                dataViewModel.SyncContext = SynchronizationContext.Current;
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
                        dataViewModel.Dispose();
                        dataViewModel.IsStarted = false;
                        dataViewModel.IsDataEnabled = false;
                        dataViewModel.IsOptionEnabled = false;
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
