using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для DockPanelPage.xaml </summary>
    public partial class DockPanelPage : Page, IDisposable, IDockablePaneProvider
    {
        public bool canceled = false;
        public ExternalEvent DataHandler;

        private bool disposedValue = false;

        public static Document CurrentDocument = null;
        private IList<RevitElementModel> familySymbolCollection = null;
        private IList<RevitDocumenModel> revitDocumentCollection = null;
        private readonly CutVoidOpeningViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly IExternalEventHandler cashExternalHandler =
        SmartToolController.Services.GetRequiredService<IExternalEventHandler>();


        public DockPanelPage()
        {
            InitializeComponent();
            Loaded += OnLoadedDockPanelPage;
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


        private void OnLoadedDockPanelPage(object sender, RoutedEventArgs e)
        {
            DataContext = dataViewModel;
            Loaded -= OnLoadedDockPanelPage;
            dataViewModel.DockPanelView = this;
            ComboDocs.SelectionChanged += ComboDocs_SelectionChanged;
            //comboSymbs.SelectionChanged += ComboSymbols_SelectionChanged;
            if (cashExternalHandler is CutVoidBaseCashHandler cashHandler)
            {
                cashHandler.Completed += OnContextSettingCompleted;
                dataViewModel.CurrentDocument = CurrentDocument;
            }
        }


        public void UpdateContext()
        {
            if (DataHandler != null)
            {
                DataHandler.Raise();
            }
        }


        private void SettingsCmd_Click(object sender, RoutedEventArgs e)
        {
            SettingsControl settingsControl = new SettingsControl(familySymbolCollection);
            Window wind = new Window { Content = settingsControl };
            wind.Show();
        }


        private void OnContextSettingCompleted(object sender, DataGroupCompletedEventArgs e)
        {
            revitDocumentCollection = e.Documents;
            familySymbolCollection = e.Elements;
            if (cashExternalHandler is CutVoidBaseCashHandler cashHandler)
            {
                cashHandler.Completed -= OnContextSettingCompleted;
                if (revitDocumentCollection != null)
                {
                    ComboDocs.ItemsSource = revitDocumentCollection;
                    CurrentDocument = revitDocumentCollection[0].Document;
                }
            }
        }


        private void ComboDocs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object item = ComboDocs.SelectedItem;
            if (item is RevitDocumenModel model)
            {
                Properties.Settings.Default.TargetDocumentName = model.Title;
                Properties.Settings.Default.Save();
            }
        }


        //private void ComboSymbols_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    object item = comboSymbs.SelectedItem;
        //    if (item is RevitElementModel model)
        //    {
        //        Properties.Settings.Default.OpennigFamilyId = model.IdInt;
        //        Properties.Settings.Default.Save();
        //    }
        //}


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckSelectAll.IsFocused == false)
            {
                ItemCollection items = dataGridView.Items;
                if (items.OfType<RevitElementModel>().All(x => x.IsSelected == true))
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
                if (items.OfType<RevitElementModel>().All(x => x.IsSelected == false))
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
            if (sender is DataGridRow row && row.DataContext is RevitElementModel model)
            {
                RevitTask.RunAsync(app =>
                {
                    UIDocument uidoc = app.ActiveUIDocument;
                    CurrentDocument = uidoc.Document;
                    Element elem = CurrentDocument.GetElement(new ElementId(model.IdInt));
                    if (elem != null && CurrentDocument is Document doc)
                    {
                        View3D view = RevitViewManager.Get3dView(doc);
                        System.Windows.Clipboard.SetText(model.IdInt.ToString());
                        RevitViewManager.IsolateElementIn3DView(uidoc, elem, view);
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
                    Content = null;
                    DataContext = null;
                    dataViewModel.Dispose();
                    if (DataHandler != null)
                    {
                        DataHandler.Dispose();
                    }
                    if (cashExternalHandler is CutVoidBaseCashHandler cashHandler)
                    {
                        cashHandler.Completed -= OnContextSettingCompleted;
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
            // Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //GC.SuppressFinalize(this);
        }


    }
}
