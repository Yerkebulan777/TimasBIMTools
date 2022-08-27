using Autodesk.Revit.DB;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RevitTimasBIMTools.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly CutOpeningOptionsViewModel settingsViewModel = ViewModelLocator.OptionsViewModel;
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = settingsViewModel;
        }


        private void CloseSettingCmd_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }


        private void advanceViz_Checked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("YES");
        }


        private void ComboCats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Category cat)
                {
                    settingsViewModel.СommunCatIdInt = cat.Id.IntegerValue;
                }
            }
        }

        private void ComboRectangSymbol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Element elem)
                {
                    settingsViewModel.RectangSymbolUniqueId = elem.UniqueId;
                }
            }
        }

        private void ComboRoundSymbol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                if (cbx.SelectionBoxItem is Element elem)
                {
                    settingsViewModel.RoundSymbolUniqueId = elem.UniqueId;
                }
            }
        }


    }


}
