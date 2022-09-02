using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using Window = System.Windows.Window;

namespace RevitTimasBIMTools.Views
{
    public partial class CutOpeningSettingsView : Window
    {
        public CutOpeningSettingsView()
        {
            InitializeComponent();           
        }


        #region SliderValueChanged

        private void DefaultSettingCmd_Click(object sender, RoutedEventArgs e)
        {
            this.sliderMinSize.Value = 30;
            this.sliderMaxSize.Value = 500;
            this.sliderCutOffset.Value = 50;
            this.sliderRatio.Value = 3;
        }

        private void MinSideSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                int value = Convert.ToInt16(slider.Value);
                slider.Value = NormilizeIntValue(value, 5, 100);
                Properties.Settings.Default.MinSideSize = value;
            }
        }

        private void MaxSideSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                int value = Convert.ToInt16(slider.Value);
                slider.Value = NormilizeIntValue(value, 100, 1500);
                Properties.Settings.Default.MaxSideSize = value;
            }
        }

        private void CutOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                int value = Convert.ToInt16(slider.Value);
                slider.Value = NormilizeIntValue(value, 0, 100);
                Properties.Settings.Default.CutOffsetInMm = value;
            }
        }

        private void Ratio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                int value = Convert.ToInt16(slider.Value);
                slider.Value = NormilizeIntValue(value, 1, 5);
                Properties.Settings.Default.Ratio = value;
            }
        }

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


        #region ComboBoxSelectionChanged

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


        private void ApplySettingCmd_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Hide();
        }

    }
}
