using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using ComboBox = System.Windows.Controls.ComboBox;


namespace RevitTimasBIMTools.Views
{
    /// <summary> Логика взаимодействия для CutVoidDockPaneView.xaml </summary>
    public partial class CutVoidDockPaneView : Page, IDockablePaneProvider
    {
        private bool Disposed { get; set; } = false;
        private readonly CutHoleDataViewModel DataContextHandler;
        private readonly string docPath = SmartToolHelper.DocumentPath;
        private readonly UIControlledApplication uicontrolapp = SmartToolApp.UIControllApp;
        private static readonly ExternalEvent externalEvent = CutHoleDataViewModel.RevitExternalEvent;

        public CutVoidDockPaneView(CutHoleDataViewModel viewModel)
        {
            InitializeComponent();
            Loaded += CutVoidDockPaneView_Loaded;
            DataContext = DataContextHandler = viewModel;
            DataContextHandler = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContextHandler.DockPanelView = this;
        }


        private void CutVoidDockPaneView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            RaiseExternalEvent();
        }


        [STAThread]
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.VisibleByDefault = false;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }


        [STAThread]
        internal void RaiseExternalEvent()
        {
            if (!DataContextHandler.IsStarted)
            {
                try
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        if (ExternalEventRequest.Accepted == externalEvent.Raise())
                        {
                            Disposed = false;
                            DataContextHandler.IsStarted = true;
                            DataContextHandler.IsOptionEnabled = false;
                            DataContextHandler.IsDataRefresh = false;
                            uicontrolapp.DockableFrameVisibilityChanged += OnDockableFrameVisibilityChanged;
                            Loaded -= CutVoidDockPaneView_Loaded;
                        }
                    });
                }
                catch (Exception ex)
                {
                    SBTLogger.Error(ex.Message);
                }
            }
        }


        private void OnDockableFrameVisibilityChanged(object sender, Autodesk.Revit.UI.Events.DockableFrameVisibilityChangedEventArgs e)
        {
            SBTLogger.Info(sender.GetType().Name);
            if (sender is CutVoidDockPaneView paneView)
            {
                _ = Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    if (!paneView.IsVisible) { Dispose(); }

                }, DispatcherPriority.Background);
            }
        }


        private void ShowModelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ElementModel model)
            {
                if (model != null && model.Instanse.IsValidObject)
                {
                    DataContextHandler.ShowElementModelView(model);
                }
            }
        }


        private void CheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DataContextHandler.ViewDataCollection.Refresh();
            Dispatcher.CurrentDispatcher.Invoke(DataContextHandler.VerifySelectDataViewCollection);
        }


        private void LoadFamily_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new()
            {
                Filter = "Family Files (*.rfa)|*.rfa",
                Title = "Open opening family",
                InitialDirectory = docPath,
                AutoUpgradeEnabled = true,
                CheckFileExists = true,
                ValidateNames = true,
                Multiselect = false,
            };

            if (DialogResult.OK == openDialog.ShowDialog())
            {
                string path = openDialog.FileName;
                if (!string.IsNullOrEmpty(path))
                {
                    DataContextHandler.LoadFamilyAsync(path);
                }
            }
        }


        private void ComboOpenning_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedValue is FamilySymbol symbol)
            {
                if (comboBox.Name.Equals(ComboWallOpenning.Name))
                {
                    Properties.Settings.Default.WallOpeningUId = symbol.UniqueId;
                    DataContextHandler.GetFamilySymbolParameterData(symbol);
                    DataContextHandler.ActivateFamilySimbol(symbol);
                    Properties.Settings.Default.Save();
                }
                if (comboBox.Name.Equals(ComboFloorOpenning.Name))
                {
                    Properties.Settings.Default.FloorOpeningUId = symbol.UniqueId;
                    DataContextHandler.GetFamilySymbolParameterData(symbol);
                    DataContextHandler.ActivateFamilySimbol(symbol);
                    Properties.Settings.Default.Save();
                }
            }
        }


        private void ComboMark_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedValue is Guid guid)
            {
                if (comboBox.Name.Equals(ComboWidthMark.Name))
                {
                    Properties.Settings.Default.WidthMarkGuid = guid;
                    Properties.Settings.Default.Save();
                }
                if (comboBox.Name.Equals(ComboHeightMark.Name))
                {
                    Properties.Settings.Default.HeightMarkGuid = guid;
                    Properties.Settings.Default.Save();
                }
                if (comboBox.Name.Equals(ComboElevMark.Name))
                {
                    Properties.Settings.Default.ElevatMarkGuid = guid;
                    Properties.Settings.Default.Save();
                }
            }
        }


        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                DataContextHandler?.Dispose();
            }
        }


    }
}
