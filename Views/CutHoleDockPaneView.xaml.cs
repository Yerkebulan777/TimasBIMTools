using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SmartBIMTools.Core;
using SmartBIMTools.RevitModel;
using SmartBIMTools.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;


namespace SmartBIMTools.Views;

/// <summary> Логика взаимодействия для CutHoleDockPaneView.xaml </summary>
public partial class CutHoleDockPaneView : Page, IDockablePaneProvider
{
    private bool Disposed { get; set; } = false;
    private readonly ExternalEvent externalEvent;
    private readonly CutHoleDataViewModel viewModel;
    private readonly string docPath = SmartToolHelper.DocumentPath;
    

    public CutHoleDockPaneView(CutHoleDataViewModel vm)
    {
        InitializeComponent();
        DataContext = viewModel = vm;
        viewModel.DockPanelView = this;
        externalEvent = CutHoleDataViewModel.RevitExternalEvent;
        viewModel = vm ?? throw new ArgumentNullException(nameof(vm));
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
        Dispatcher.CurrentDispatcher.Invoke(() =>
        {
            viewModel.Dispose();
            ExternalEventRequest request = externalEvent.Raise();
            if (ExternalEventRequest.Accepted == request)
            {
                Disposed = false;
                viewModel.IsStarted = true;
                viewModel.IsOptionEnabled = false;
                viewModel.IsDataRefresh = false;
            }
        }, DispatcherPriority.Background);
    }


    private void ShowModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ElementModel model)
        {
            if (model != null && model.IsValidModel())
            {
                viewModel.ShowElementModelView(model);
            }
        }
    }


    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
        viewModel.ViewDataCollection.Refresh();
        Dispatcher.CurrentDispatcher.Invoke(viewModel.VerifySelectDataViewCollection);
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
                viewModel.LoadFamilyAsync(path);
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
                viewModel.GetFamilySymbolParameterData(symbol);
                viewModel.ActivateFamilySimbol(symbol);
                Properties.Settings.Default.Save();
            }
            if (comboBox.Name.Equals(ComboFloorOpenning.Name))
            {
                Properties.Settings.Default.FloorOpeningUId = symbol.UniqueId;
                viewModel.GetFamilySymbolParameterData(symbol);
                viewModel.ActivateFamilySimbol(symbol);
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
            ActiveDocTitle.Content = null;
            viewModel?.Dispose();
        }
    }
}
