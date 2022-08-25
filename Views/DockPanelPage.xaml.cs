﻿using Autodesk.Revit.DB;
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
        public Document CurrentDocument { get; set; } = null;

        private bool disposedValue = false;
        private RevitDocumenModel revitDocumentModel;
        private IList<RevitDocumenModel> revitDocumentModeList = null;
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly CutOpeningOptionsViewModel optViewModel = ViewModelLocator.OptionsViewModel;
        private readonly SettingsWindow settingsControl = SmartToolController.Services.GetRequiredService<SettingsWindow>();
        private readonly CutOpeningMainHandler viewHandler = SmartToolController.Services.GetRequiredService<CutOpeningMainHandler>();

        public DockPanelPage()
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
            revitDocumentModeList = e.Documents;
            revitDocumentModel = revitDocumentModeList.FirstOrDefault();
            if (CurrentDocument == null && revitDocumentModeList.Count > 0)
            {
                ActiveDocTitle.Content = revitDocumentModel.Document.Title.ToUpper();
                dataViewModel.CurrentDocument = revitDocumentModel.Document;
                optViewModel.CurrentDocument = revitDocumentModel.Document;
                ComboDocs.SelectionChanged += ComboDocs_SelectionChanged;
                viewHandler.Completed -= OnContextViewHandlerCompleted;
                ComboDocs.ItemsSource = revitDocumentModeList;
                CurrentDocument = revitDocumentModel.Document;
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


        private async void SettingsCmd_ClickAsync(object sender, RoutedEventArgs e)
        {
            await optViewModel.RaiseExternalEventAsync();
            ShowSettingsWindow(settingsControl);
        }

        private void ShowSettingsWindow(SettingsWindow settingsControl)
        {
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
                System.Threading.Tasks.Task task = RevitTask.RunAsync(app =>
                {
                    UIDocument uidoc = app.ActiveUIDocument;
                    CurrentDocument = uidoc.Document;
                    Element elem = CurrentDocument.GetElement(new ElementId(model.IdInt));
                    if (elem != null && CurrentDocument is Document doc)
                    {
                        View3D view = RevitViewManager.Get3dView(uidoc);
                        System.Windows.Clipboard.SetText(model.IdInt.ToString());
                        RevitViewManager.IsolateElementIn3DView(uidoc, elem, view);
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
