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
        }



        #region SelectionChanged

        private void ComboCats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Category cat)
                {
                    Properties.Settings.Default.СommunCatIdInt = cat.Id.IntegerValue;
                }
            }
        }

        private void ComboRectangSymbol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Element elem)
                {
                    Properties.Settings.Default.RectangSymbolUniqueId = elem.UniqueId;
                }
            }
        }

        private void ComboRoundSymbol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Element elem)
                {
                    Properties.Settings.Default.RoundSymbolUniqueId = elem.UniqueId;
                }
            }
        }

        #endregion


        private void DefaultSettingCmd_Click(object sender, RoutedEventArgs e)
        {
            optViewModel.MinElementSize = 30;
            optViewModel.MaxElementSize = 500;
            optViewModel.CutOffset = 50;
            optViewModel.Ratio = 3;
        }

        private void ApplySettingCmd_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Hide();
        }

        #region Methods

        private static int NormilizeIntValue(int value, int minVal = 0, int maxVal = 100)
        {
            if (value < minVal)
            {
                value = minVal;
            }
            if (value > maxVal)
            {
                value = maxVal;
            }
            return value;
        }

        #endregion

    }
}
