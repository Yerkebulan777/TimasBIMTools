using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace RevitTimasBIMTools.Views
{
    public partial class SettingsWindow : Window
    {
        private Element element { get; set; } = null;
        private readonly SettingsViewModel settingsViewModel = ViewModelLocator.SettingsViewModel;
        private readonly CutOpeningSettingsHandler settingsHandler = SmartToolController.Services.GetRequiredService<CutOpeningSettingsHandler>();
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = settingsViewModel;
            Loaded += SettingsWindow_Loaded;
            settingsHandler.Completed += OnContextViewHandlerCompleted;
        }
                
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }
        
        private void OnContextViewHandlerCompleted(object sender, SettingsCompletedEventArgs e)
        {
            settingsViewModel.RevitCategories = new ObservableCollection<Category>(e.Categories);
            settingsViewModel.RevitFamilySimbols = new ObservableCollection<FamilySymbol>(e.Symbols);
            //Task.Delay(100).ContinueWith(task => RevitLogger.Info($"Categories = {e.Categories.Count}"));
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
