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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutOpeningDockPanelView.xaml </summary>
    public partial class CutOpeningDockPanelView : Page, IDisposable, IDockablePaneProvider
    {
        public UIDocument CurrentUIDocument { get; set; } = null;
        public View3D View3d { get; set; } = null;

        private bool disposedValue = false;
        private DocumentModel documentModel;
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly CutOpeningStartHandler viewHandler = SmartToolController.Services.GetRequiredService<CutOpeningStartHandler>();
        private readonly CutOpeningSettingsView settingsView = SmartToolController.Services.GetRequiredService<CutOpeningSettingsView>();
        
        public CutOpeningDockPanelView()
        {
            InitializeComponent();
            DataContext = dataViewModel;
            dataViewModel.DockPanelView = this;
            viewHandler.Completed += OnContextViewHandlerCompleted;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
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
            CurrentUIDocument = args.CurrentUIDocument;
            documentModel = args.Documents.FirstOrDefault();
            ComboDocs.ItemsSource = args.Documents;
            if (documentModel.IsActive)
            {
                viewHandler.Completed -= OnContextViewHandlerCompleted;
                ComboDocs.SelectionChanged += ComboDocs_SelectionChanged;
                settingsView.ComboTargetCats.ItemsSource = args.Categories;
                settingsView.ComboRoundSymbol.ItemsSource = args.FamilySymbols;
                settingsView.ComboRectangSymbol.ItemsSource = args.FamilySymbols;
                settingsView.ComboStructMats.ItemsSource = args.StructureMaterials;
                ActiveDocTitle.Content = documentModel.Document.Title.ToUpper();
                dataViewModel.CurrentDocument = documentModel.Document;
            }
        }


        private void ShowSettingsCmd_Click(object sender, RoutedEventArgs e)
        {
            if (true == settingsView.ShowDialog() && settingsView.Activate())
            {
                Task.Delay(1000).Wait();
            }
        }


        private void ComboDocs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object item = ComboDocs.SelectedItem;
            if (item is DocumentModel model)
            {
                Properties.Settings.Default.TargetDocumentName = model.Title;
                Properties.Settings.Default.Save();
            }
        }


        #region CheckSelectAll
        private void CheckSelectAll_CheckChanged(object sender, RoutedEventArgs e)
        {
            ItemCollection items = dataGridView.Items;
            if (sender is CheckBox chkSelectAll)
            {
                if (items.IsEmpty)
                {
                    chkSelectAll.IsChecked = false;
                }
                else if (chkSelectAll.IsChecked == true)
                {
                    items.OfType<ElementModel>().ToList().ForEach(x => x.IsSelected = true);
                }
                else if (chkSelectAll.IsChecked == false)
                {
                    items.OfType<ElementModel>().ToList().ForEach(x => x.IsSelected = false);
                }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ItemCollection items = dataGridView.Items;
            dataViewModel.IsAllSelectChecked = items.OfType<ElementModel>().All(x => x.IsSelected == true) ? true : (bool?)null;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ItemCollection items = dataGridView.Items;
            dataViewModel.IsAllSelectChecked = items.OfType<ElementModel>().All(x => x.IsSelected == false) ? false : (bool?)null;
        }

        #endregion


        [STAThread]
        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                Task task = RevitTask.RunAsync(app =>
                {
                    Document doc = app.ActiveUIDocument.Document;
                    if (CurrentUIDocument is UIDocument uidoc && uidoc.Document.Title.Equals(doc.Title))
                    {
                        Element elem = doc.GetElement(new ElementId(model.IdInt));
                        System.Windows.Clipboard.SetText(model.IdInt.ToString());
                        RevitViewManager.ShowElement(CurrentUIDocument, elem);
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
                    ComboDocs.SelectionChanged -= ComboDocs_SelectionChanged;
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
