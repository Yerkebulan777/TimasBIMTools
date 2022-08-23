using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace RevitTimasBIMTools.Views
{
    public partial class SettingsWindow : Window
    {

        private ObservableCollection<Category> categories = null;
        private readonly SettingsViewModel settingsViewModel = ViewModelLocator.SettingsViewModel;
        private readonly IExternalEventHandler settingsViewHandler = SmartToolController.Services.GetRequiredService<CutOpeningBaseHandler>();
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = settingsViewModel;
            if (settingsViewHandler is CutOpeningBaseHandler handler)
            {
                handler.Completed += OnContextViewHandlerCompleted;
                settingsViewModel.Categories = categories;
            }
        }


        private void OnContextViewHandlerCompleted(object sender, BaseCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }


        private void CloseSettingCmd_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Close();

        }


        private void advanceViz_Checked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("YYYYES");
        }
    }
}
