using RevitTimasBIMTools.ViewModels;
using System;
using System.Windows;

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
            Close();
        }


        private void advanceViz_Checked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("YES");
        }
    }
}
