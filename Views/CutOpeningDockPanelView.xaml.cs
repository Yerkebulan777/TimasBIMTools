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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutOpeningDockPanelView.xaml </summary>
    public partial class CutOpeningDockPanelView : Page, IDisposable, IDockablePaneProvider
    {
        public string CurrentDocumentGuid { get; set; } = null;
        public View3D View3d { get; set; } = null;

        private DispatcherTimer timer;
        private double gridWidthSize = 0;
        private bool disposedValue = false;
        private DocumentModel documentModel = null;
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        private readonly CutOpeningStartHandler viewHandler = SmartToolController.Services.GetRequiredService<CutOpeningStartHandler>();
        private readonly CutOpeningSettingsView settingsView = SmartToolController.Services.GetRequiredService<CutOpeningSettingsView>();


        public CutOpeningDockPanelView()
        {
            InitializeComponent();
            DataContext = dataViewModel;
            SizeChanged += OnViewSizeChanged;
            dataViewModel.DockPanelView = this;
            viewHandler.Completed += OnContextViewHandlerCompleted;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.Tick += Timer_Tick;
            sidePanel.Width = 0;
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


        private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                sidePanel.Width = 0;
            }
        }


        #region TickTimer
        private void Timer_Tick(object sender, EventArgs e)
        {
            gridWidthSize = viewPage.Width;
            if (sidePanel.Width < gridWidthSize)
            {
                sidePanel.Width += 0.5;
                if (sidePanel.Width > gridWidthSize)
                {
                    timer.Stop();
                }
            }
            else
            {
                sidePanel.Width -= 0.5;
                if (sidePanel.Width > 1)
                {
                    timer.Stop();
                }
            }
        }
        #endregion


        private void OnContextViewHandlerCompleted(object sender, BaseCompletedEventArgs args)
        {
            View3d = args.View3d;
            documentModel = args.Documents.FirstOrDefault();
            CurrentDocumentGuid = args.CurrentDocumentGuid;
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
                dataViewModel.CurrentDocument = documentModel.Document;
            }
        }


        private void ShowSettingsCmd_Click(object sender, RoutedEventArgs e)
        {
            Task task = RevitTask.RunAsync(app => timer.Start());
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
                    if (CurrentDocumentGuid.Equals(doc.ProjectInformation.UniqueId))
                    {
                        Element elem = doc.GetElement(new ElementId(model.IdInt));
                        System.Windows.Clipboard.SetText(model.IdInt.ToString());
                        RevitViewManager.ShowElement(app.ActiveUIDocument, elem);
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
