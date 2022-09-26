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
using Xceed.Wpf.Toolkit;

namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutOpeningDockPanelView.xaml </summary>
    public partial class CutOpeningDockPanelView : Page, IDisposable, IDockablePaneProvider
    {

        public View3D View3d { get; set; } = null;

        private bool disposedValue = false;
        private DocumentModel documentModel = null;

        private readonly Mutex mutex = new Mutex();
        public IList<FamilySymbol> HostedFamilySymbols = new List<FamilySymbol>(25);
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly string currentDocumentId = Properties.Settings.Default.CurrentDocumentUniqueId;
        private readonly CutOpeningStartHandler viewHandler = SmartToolController.Services.GetRequiredService<CutOpeningStartHandler>();


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
            documentModel = args.DocumentModels.FirstOrDefault();
            if (documentModel.IsActive)
            {
                ComboDocumentModels.ItemsSource = args.DocumentModels;
                viewHandler.Completed -= OnContextViewHandlerCompleted;
                ActiveDocTitle.Content = Properties.Settings.Default.TargetDocumentName.ToUpper();
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
                        ComboLevelFilter.ItemsSource = RevitFilterManager.GetValidLevels(doc);
                        ComboEngineerCats.ItemsSource = RevitFilterManager.GetEngineerCategories(doc);
                        ComboStructureMats.ItemsSource = RevitMaterialManager.GetAllConstructionStructureMaterials(doc);
                        HostedFamilySymbols = RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel).ToList();
                        ComboRectangSymbol.ItemsSource = HostedFamilySymbols;
                        ComboRoundedSymbol.ItemsSource = HostedFamilySymbols;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    dataViewModel.IsOptionsEnabled = false;
                    dataViewModel.IsDataEnabled = true;
                }
            });
        }


        private void Integer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is IntegerUpDown controler && controler.Name is string name)
            {
                int value = Convert.ToInt32(e.NewValue);
                if (name.Equals("MinSideSize"))
                {
                    Properties.Settings.Default.MinSideSizeInMm = value;
                }
                if (name.Equals("MaxSideSize"))
                {
                    Properties.Settings.Default.MaxSideSizeInMm = value;
                }
                if (name.Equals("CutOffset"))
                {
                    Properties.Settings.Default.CutOffsetInMm = value;
                }
            }
        }


        [STAThread]
        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                Task task = RevitTask.RunAsync(app =>
                {
                    Document doc = app.ActiveUIDocument.Document;
                    if (currentDocumentId.Equals(doc.ProjectInformation.UniqueId))
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
