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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Level = Autodesk.Revit.DB.Level;

namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutOpeningDockPanelView.xaml </summary>
    public partial class CutOpeningDockPanelView : Page, IDisposable, IDockablePaneProvider
    {
        public string DocumentGuid { get; set; } = null;
        public View3D View3d { get; set; } = null;

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
                //settingsView.ComboTargetCats.ItemsSource = args.Categories;
                //settingsView.ComboRoundSymbol.ItemsSource = args.FamilySymbols;
                //settingsView.ComboRectangSymbol.ItemsSource = args.FamilySymbols;
                //settingsView.ComboStructMats.ItemsSource = args.StructureMaterials;
                dataViewModel.DocumentModels = args.Documents.ToObservableCollection();
                ActiveDocTitle.Content = documentModel.Document.Title.ToUpper();
            }
        }


        private void ShowSettingsCmd_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                if (!dataViewModel.IsOptionsEnabled)
                {
                    Document doc = null;
                    Task t = RevitTask.RunAsync(async app =>
                    {
                        doc = app.ActiveUIDocument.Document;
                        dataViewModel.IsDataEnabled = false;
                        dataViewModel.IsOptionsEnabled = true;
                        await Task.Delay(1000).ConfigureAwait(true);
                    })
                    .ContinueWith(app =>
                    {
                        SortedList<double, Level> levels = new SortedList<double, Level>();
                        foreach (Level lvl in RevitFilterManager.GetValidLevels(doc))
                        {
                            levels[lvl.ProjectElevation] = lvl;
                        }
                        ComboFloorFilter.ItemsSource = levels;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    dataViewModel.IsOptionsEnabled = false;
                    dataViewModel.IsDataEnabled = true;
                }
            });
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
                    Properties.Settings.Default.Reset();
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
