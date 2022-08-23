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
    /// <summary> Логика взаимодействия для DockPanelPage.xaml </summary>
    public partial class DockPanelPage : Page, IDisposable, IDockablePaneProvider
    {
        public bool canceled = false;
        public ExternalEvent DataHandler;

        private bool disposedValue = false;

        private RevitDocumenModel revitDocumentModel;
        public static Document CurrentDocument = null;
        private IList<RevitDocumenModel> revitDocumentCollection = null;
        private readonly CutOpeningViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly IExternalEventHandler openingViewHandler = SmartToolController.Services.GetRequiredService<CutOpeningBaseHandler>();

        public DockPanelPage()
        {
            InitializeComponent();
            DataContext = dataViewModel;
            dataViewModel.DockPanelView = this;
            if (openingViewHandler is CutOpeningBaseHandler handler)
            {
                handler.Completed += OnContextViewHandlerCompleted;
            }
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
            if (openingViewHandler is CutOpeningBaseHandler handler)
            {
                revitDocumentCollection = e.Documents;
                revitDocumentModel = revitDocumentCollection.FirstOrDefault();
                if (CurrentDocument == null && revitDocumentCollection.Count > 0)
                {
                    CurrentDocument = revitDocumentModel.Document;
                    dataViewModel.CurrentDocument = CurrentDocument;
                    ComboDocs.ItemsSource = revitDocumentCollection;
                    ComboDocs.SelectionChanged += ComboDocs_SelectionChanged;
                    handler.Completed -= OnContextViewHandlerCompleted;
                }
            }
        }


        public void UpdateContext()
        {
            if (DataHandler != null)
            {
                _ = DataHandler.Raise();
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


        private void SettingsCmd_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsControl = new SettingsWindow();
            settingsControl.Show();
        }


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
                _ = RevitTask.RunAsync(app =>
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
