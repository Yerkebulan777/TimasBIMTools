using System;
using System.Windows;

namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void CloseSettingCmd_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();

        }

        private void advanceViz_Checked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("YYYYES");
        }
    }
}
