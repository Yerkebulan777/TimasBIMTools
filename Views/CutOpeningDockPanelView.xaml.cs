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

namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutOpeningDockPanelView.xaml </summary>
    public partial class CutOpeningDockPanelView : Page, IDisposable, IDockablePaneProvider
    {

        private bool disposedValue = false;
        private readonly Mutex mutex = new Mutex();
        public View3D View3d { get; set; } = null;
        public DocumentModel ActiveDocModel = null;

        private IDictionary<string, FamilySymbol> familySymbols = null;
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly string documentId = Properties.Settings.Default.CurrentDocumentUniqueId;
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
                TabBehind = DockablePanes.BuiltInDockablePanes.PropertiesPalette
            };
        }


        private void OnContextViewHandlerCompleted(object sender, BaseCompletedEventArgs args)
        {
            View3d = args.View3d;
            ActiveDocModel = args.DocumentModels.FirstOrDefault();
            viewHandler.Completed -= OnContextViewHandlerCompleted;
            ActiveDocTitle.Content = ActiveDocModel.Title.ToUpper();
            ComboDocumentModels.ItemsSource = args.DocumentModels;
            ComboDocumentModels.SelectedIndex = 0;
        }


        private void ShowSettingsCmd_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Document doc = null;
                string docId = string.Empty;
                Task task = !dataViewModel.IsOptionsEnabled
                    ? RevitTask.RunAsync(async app =>
                    {
                        doc = app.ActiveUIDocument.Document;
                        dataViewModel.IsDataEnabled = false;
                        dataViewModel.IsOptionsEnabled = true;
                        docId = doc.ProjectInformation.UniqueId;
                        await Task.Delay(1000).ConfigureAwait(true);
                    })
                    .ContinueWith(app =>
                    {
                        if (documentId.Equals(docId))
                        {
                            ComboEngineerCats.ItemsSource = RevitFilterManager.GetEngineerCategories(doc);
                            ComboStructureMats.ItemsSource = RevitFilterManager.GetAllConstructionStructureMaterials(doc);
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
                        docId = doc.ProjectInformation.UniqueId;
                        await Task.Delay(1000).ConfigureAwait(true);
                    })
                    .ContinueWith(app =>
                    {
                        if (documentId.Equals(docId))
                        {
                            ComboLevelFilter.ItemsSource = RevitFilterManager.GetValidLevels(doc);
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
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
