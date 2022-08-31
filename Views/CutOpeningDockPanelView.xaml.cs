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
using System.Windows;
using System.Windows.Controls;


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutOpeningDockPanelView.xaml </summary>
    public partial class CutOpeningDockPanelView : Page, IDisposable, IDockablePaneProvider
    {
        public Document CurrentDocument { get; set; } = null;

        private bool disposedValue = false;
        private DocumentModel documentModel;
        private IList<DocumentModel> documentModeList = null;
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly CutOpeningSettingsViewModel optViewModel = ViewModelLocator.SettingsViewModel;
        private readonly CutOpeningSettingsView settingsView = SmartToolController.Services.GetRequiredService<CutOpeningSettingsView>();
        private readonly CutOpeningStartHandler viewHandler = SmartToolController.Services.GetRequiredService<CutOpeningStartHandler>();

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


        private void OnContextViewHandlerCompleted(object sender, BaseCompletedEventArgs e)
        {
            documentModeList = e.Documents;
            documentModel = documentModeList.FirstOrDefault();
            if (CurrentDocument == null && documentModel.IsActive)
            {
                settingsView.ComboTargetCats.ItemsSource = e.Categories;
                settingsView.ComboRoundSymbol.ItemsSource = e.FamilySymbols;
                settingsView.ComboRectangSymbol.ItemsSource = e.FamilySymbols;
                settingsView.ComboStructMats.ItemsSource = e.StructureMaterials;
                ActiveDocTitle.Content = documentModel.Document.Title.ToUpper();
                dataViewModel.CurrentDocument = documentModel.Document;
                ComboDocs.SelectionChanged += ComboDocs_SelectionChanged;
                viewHandler.Completed -= OnContextViewHandlerCompleted;
                ComboDocs.ItemsSource = documentModeList;
                CurrentDocument = documentModel.Document;
            }
        }


        private void ShowSettingsCmd_Click(object sender, RoutedEventArgs e)
        {
            if (true == settingsView.ShowDialog())
            {
                _ = settingsView.Activate();
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


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckSelectAll.IsFocused == false)
            {
                ItemCollection items = dataGridView.Items;
                if (items.OfType<ElementModel>().All(x => x.IsSelected == true))
                {
                    CheckSelectAll.IsChecked = true;
                }
                else
                {
                    dataViewModel.HandleSelectAllCommand(null);
                }
            }
        }


        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (CheckSelectAll.IsFocused == false)
            {
                ItemCollection items = dataGridView.Items;
                if (items.OfType<ElementModel>().All(x => x.IsSelected == false))
                {
                    CheckSelectAll.IsChecked = false;
                }
                else
                {
                    dataViewModel.HandleSelectAllCommand(null);
                }
            }
        }


        [STAThread]
        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ElementModel model)
            {
                System.Threading.Tasks.Task task = RevitTask.RunAsync(app =>
                {
                    UIDocument uidoc = app.ActiveUIDocument;
                    CurrentDocument = uidoc.Document;
                    Element elem = CurrentDocument.GetElement(new ElementId(model.IdInt));
                    if (elem != null && CurrentDocument is Document doc)
                    {
                        RevitViewManager.ShowAndZoomElement(uidoc, elem);
                        System.Windows.Clipboard.SetText(model.IdInt.ToString());
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
