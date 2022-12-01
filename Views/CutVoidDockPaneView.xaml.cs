using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using Document = Autodesk.Revit.DB.Document;

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
                Family family = null;
                _ = RevitTask.RunAsync(app =>
                {
                    Document doc = app.ActiveUIDocument.Document;
                    using Transaction trx = new(doc, "Load Family");
                    TransactionStatus status = trx.Start();
                    if (status == TransactionStatus.Started)
                    {
                        if (doc.LoadFamily(openDialog.FileName, out family))
                        {
                            status = trx.Commit();
                            Document familyDocument = doc.EditFamily(family);
                            string path = Path.Combine(docPath, "SmartBIMTool");
                            if (!Directory.Exists(path)) { _ = Directory.CreateDirectory(path); }
                            SaveAsOptions options = new() { OverwriteExistingFile = true };
                            familyDocument.SaveAs(@$"{path}\{family.Name}.rfa", options);
                            foreach (ElementId symbId in family.GetFamilySymbolIds())
                            {
                                Element symbol = doc.GetElement(symbId);
                                string symbName = symbol.Name;
                            }
                        }
                    }
                });
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
