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


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutVoidDockPanelView.xaml </summary>
    public partial class CutVoidDockPanelView : Page, IDockablePaneProvider, IDisposable
    {
        private bool disposedValue = false;
        private readonly Mutex mutex = new();
        private CutVoidViewExternalHandler handler { get; set; } = null;
        private ExternalEvent externalEvent { get; set; } = null;

        private readonly IServiceProvider provider = ContainerConfig.ConfigureServices();
        private readonly CutVoidDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly string documentId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly TaskScheduler syncContext = TaskScheduler.FromCurrentSynchronizationContext();

        public CutVoidDockPanelView()
        {
            InitializeComponent();
            DataContext = dataViewModel;
            dataViewModel.DockPanelView = this;
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
            handler = provider.GetRequiredService<CutVoidViewExternalHandler>();
            handler.Completed += OnContextHandlerCompleted;
            externalEvent = ExternalEvent.Create(handler);

            if (ExternalEventRequest.Accepted != externalEvent.Raise())
            {
                Logger.Warning("External event request not accepted!!!");
            }
        }


        private void OnContextHandlerCompleted(object sender, BaseCompletedEventArgs args)
        {
            dataViewModel.IsStarted = true;
            dataViewModel.DocumentModelCollection = args.DocumentModels;
            dataViewModel.ConstructionTypeIds = args.ConstructionTypeIds;
        }


        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                _ = RevitTask.RunAsync(app =>
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


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                    Task task = Task.Run(dataViewModel.Dispose)
                    .ContinueWith(_ =>
                    {
                        CommandManager.InvalidateRequerySuggested();
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
