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
        private bool disposedValue = false;
        private readonly Mutex mutex = new();
        private IDictionary<string, FamilySymbol> familySymbols = null;
        private readonly string documentId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly CutOpeningStartExternalHandler viewHandler = SmartToolController.Services.GetRequiredService<CutOpeningStartExternalHandler>();


        public CutOpeningDockPanelView()
        {
            InitializeComponent();
            DataContext = dataViewModel;
            dataViewModel.DockPanelView = this;
            Properties.Settings.Default.Reset();
            viewHandler.Completed += OnContextHandlerCompleted;
            Dispatcher.CurrentDispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }


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
        private void OnContextHandlerCompleted(object sender, BaseCompletedEventArgs args)
        {
            dataViewModel.IsStarted = true;
            dataViewModel.ConstructionTypeIds = args.ConstructionTypeIds;
            dataViewModel.DocModelCollection = args.DocumentModels.ToObservableCollection();
            viewHandler.Completed -= OnContextHandlerCompleted;
        }


        [STAThread]
        private void ShowSettingsCmd_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Document doc = null;
                dataViewModel.ElementModelData.Clear();
                Task task = !dataViewModel.IsOptionsEnabled
                    ? RevitTask.RunAsync(async app =>
                    {
                        doc = app.ActiveUIDocument.Document;
                        dataViewModel.IsDataEnabled = false;
                        dataViewModel.IsOptionsEnabled = true;
                        await Task.Delay(1000).ConfigureAwait(true);
                    })
                    .ContinueWith(app =>
                    {
                        if (documentId.Equals(doc.ProjectInformation.UniqueId))
                        {
                            ComboEngineerCats.ItemsSource = RevitFilterManager.GetEngineerCategories(doc);
                            ComboStructureMats.ItemsSource = RevitFilterManager.GetConstructionCoreMaterials(doc, dataViewModel.ConstructionTypeIds);
                            familySymbols = RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel);
                            ComboRectangSymbol.ItemsSource = familySymbols;
                            ComboRoundedSymbol.ItemsSource = familySymbols;
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext())
                    : RevitTask.RunAsync(async app =>
                    {
                        doc = app.ActiveUIDocument.Document;
                        dataViewModel.IsDataEnabled = true;
                        dataViewModel.IsOptionsEnabled = false;
                        await Task.Delay(1000).ConfigureAwait(true);
                    })
                    .ContinueWith(app =>
                    {
                        if (documentId.Equals(doc.ProjectInformation.UniqueId))
                        {
                            ComboLevelFilter.ItemsSource = RevitFilterManager.GetValidLevels(doc);
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
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
                    Task task = RevitTask.RunAsync(app =>
                    {
                        Properties.Settings.Default.Reset();
                    })
                    .ContinueWith(app =>
                    {
                        dataViewModel?.Dispose();
                    }, TaskScheduler.FromCurrentSynchronizationContext());
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
