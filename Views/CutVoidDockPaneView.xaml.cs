using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Document = Autodesk.Revit.DB.Document;

namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutVoidDockPaneView.xaml </summary>
    public partial class CutVoidDockPaneView : Page, IDockablePaneProvider, IDisposable
    {
        private readonly Mutex mutex = new();
        public bool Disposed { get; set; } = false;
        private string documentId { get; set; } = Properties.Settings.Default.ActiveDocumentUniqueId;

        private readonly CutVoidDataViewModel DataContextHandler = null;
        private readonly SmartToolHelper helper = SmartToolApp.ServiceProvider.GetRequiredService<SmartToolHelper>();
        public CutVoidDockPaneView(CutVoidDataViewModel viewModel)
        {
            InitializeComponent();
            Loaded += CutVoidDockPaneView_Loaded;
            DataContext = DataContextHandler = viewModel;
            viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Logger.ThreadProcessLog("Process => " + nameof(CutVoidDockPaneView));
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
        private void CutVoidDockPaneView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContextHandler != null)
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    DataContextHandler.IsStarted = true;
                    DataContextHandler.DockPanelView = this;
                });
            }
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
                    Properties.Settings.Default.Reset();
                }, DispatcherPriority.Background);
                Dispatcher.CurrentDispatcher.InvokeShutdown();
                // TODO: освободить управляемое состояние (управляемые объекты)
            }
        }
    }
}
