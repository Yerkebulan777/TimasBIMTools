using Autodesk.Revit.UI;
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
        private readonly CutVoidDataViewModel DataContextHandler;
        private static readonly ExternalEvent externalEvent = CutVoidDataViewModel.RevitExternalEvent;

        public CutVoidDockPaneView(CutVoidDataViewModel viewModel)
        {
            InitializeComponent();
            Loaded += CutVoidDockPaneView_Loaded;
            DataContext = DataContextHandler = viewModel;
            DataContextHandler = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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
                            DataContextHandler.DockPanelView = this;
                            DataContextHandler.IsOptionEnabled = false;
                            DataContextHandler.IsDataRefresh = false;
                            Loaded -= CutVoidDockPaneView_Loaded;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
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
            Dispatcher.CurrentDispatcher.Invoke(DataContextHandler.VerifySelectDataViewCollection);
        }


        private void LoadFamily_Click(object sender, RoutedEventArgs e)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
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
                    DataContextHandler.LoadFamily(path);
                }
            }
        }


        private void ComboMark_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedValue is Guid guid)
            {
                if (comboBox.Name.Equals("ComboWidthMark"))
                {
                    Properties.Settings.Default.WidthMarkGuid = guid;
                    Properties.Settings.Default.Save();
                }
                if (comboBox.Name.Equals("ComboHeightMark"))
                {
                    Properties.Settings.Default.HeightMarkGuid = guid;
                    Properties.Settings.Default.Save();
                }
                if (comboBox.Name.Equals("ComboElevMark"))
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
