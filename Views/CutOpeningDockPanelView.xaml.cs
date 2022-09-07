using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutOpeningDockPanelView.xaml </summary>
    public partial class CutOpeningDockPanelView : Page, IDisposable, IDockablePaneProvider
    {
        public string DocumentGuid { get; set; } = null;
        public View3D View3d { get; set; } = null;

        private bool flag;
        private bool disposedValue = false;
        private readonly Mutex mutex = new Mutex();
        private DocumentModel documentModel = null;
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly CutOpeningStartHandler viewHandler = SmartToolController.Services.GetRequiredService<CutOpeningStartHandler>();
        private readonly CutOpeningSettingsView settingsView = SmartToolController.Services.GetRequiredService<CutOpeningSettingsView>();


        public CutOpeningDockPanelView()
        {
            InitializeComponent();
            DataContext = dataViewModel;
            dataViewModel.DockPanelView = this;
            viewHandler.Completed += OnContextViewHandlerCompleted;
            Dispatcher.CurrentDispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }


        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }


        private void OnContextViewHandlerCompleted(object sender, BaseCompletedEventArgs args)
        {
            View3d = args.View3d;
            DocumentGuid = args.CurrentDocumentGuid;
            documentModel = args.Documents.FirstOrDefault();
            dataViewModel.DocumentGuid = args.CurrentDocumentGuid;
            if (documentModel.IsActive)
            {
                viewHandler.Completed -= OnContextViewHandlerCompleted;
                //ComboDocs.SelectionChanged += ComboDocs_SelectionChanged;
                settingsView.ComboTargetCats.ItemsSource = args.Categories;
                settingsView.ComboRoundSymbol.ItemsSource = args.FamilySymbols;
                settingsView.ComboRectangSymbol.ItemsSource = args.FamilySymbols;
                settingsView.ComboStructMats.ItemsSource = args.StructureMaterials;
                dataViewModel.DocumentModels = args.Documents.ToObservableCollection();
                ActiveDocTitle.Content = documentModel.Document.Title.ToUpper();
            }
        }


        private void ShowSettingsCmd_Cick(object sender, RoutedEventArgs e)
        {
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate ()
            {
                if (mutex.WaitOne(5000))
                {
                    if (sidePanel.Opacity == 0)
                    {
                        sidePanel.Opacity = 1;
                        dataViewModel.IsOptionsEnabled = true;
                    }
                    else
                    {
                        sidePanel.Opacity = 0;
                        dataViewModel.IsOptionsEnabled = false;
                    }
                }
                mutex.ReleaseMutex();
            });
        }


        private void ComboDocs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //object item = ComboDocs.SelectedItem;
            //if (item is DocumentModel model)
            //{
            //    Properties.Settings.Default.TargetDocumentName = model.Title;
            //    Properties.Settings.Default.Save();
            //}
        }


        [STAThread]
        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                Task task = RevitTask.RunAsync(app =>
                {
                    Document doc = app.ActiveUIDocument.Document;
                    if (DocumentGuid.Equals(doc.ProjectInformation.UniqueId))
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


        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Dispatcher.ShutdownStarted -= Dispatcher_ShutdownStarted;
            Dispose();
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Content = null;
                    DataContext = null;
                    dataViewModel.Dispose();
                    //ComboDocs.SelectionChanged -= ComboDocs_SelectionChanged;
                    for (int i = 0; i < PageMainGrid.Children.Count; i++)
                    {
                        PageMainGrid.Children.Remove(PageMainGrid.Children[i]);
                    }
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
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //GC.SuppressFinalize(this);
        }


    }
}
