using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;


namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private readonly SettingsViewModel settingsViewModel = ViewModelLocator.SettingsViewModel;
        public readonly IList<RevitElementModel> FamilySymbolCollection = null;
        public SettingsControl(IList<RevitElementModel> models)
        {
            FamilySymbolCollection = models;
            Loaded += OnLoadedSettingsView;
            InitializeComponent();
        }

        private void OnLoadedSettingsView(object sender, RoutedEventArgs e)
        {
            DataContext = settingsViewModel;
            Loaded -= OnLoadedSettingsView;
        }


        //private void ComboSymbols_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    object item = comboSymbs.SelectedItem;
        //    if (item is RevitElementModel model)
        //    {
        //        Properties.Settings.Default.OpennigFamilyId = model.IdInt;
        //        Properties.Settings.Default.Save();
        //    }
        //}

    }
}
