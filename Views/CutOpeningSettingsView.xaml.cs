using Autodesk.Revit.DB;
using RevitTimasBIMTools.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RevitTimasBIMTools.Views
{
    public partial class CutOpeningSettingsView : Window
    {
        private readonly CutOpeningSettingsViewModel optViewModel = ViewModelLocator.SettingsViewModel;
        public CutOpeningSettingsView()
        {
            InitializeComponent();
            DataContext = optViewModel;
            optViewModel.SettingsView = this;
        }


        private void ComboCats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Category cat)
                {
                    optViewModel.СommunCatIdInt = cat.Id.IntegerValue;
                }
            }
        }

        private void ComboRectangSymbol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Element elem)
                {
                    optViewModel.RectangSymbolUniqueId = elem.UniqueId;
                }
            }
        }

        private void ComboRoundSymbol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Element elem)
                {
                    optViewModel.RoundSymbolUniqueId = elem.UniqueId;
                }
            }
        }

        private void ApplySettingCmd_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Hide();
        }
    }
}
