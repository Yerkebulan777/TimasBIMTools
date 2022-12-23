using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Interaction logic for AreaRebarMarkFixWindow.xaml
    /// </summary>

    public partial class AreaRebarMarkFixWindow : Window
    {
        public AreaRebarMarkFixWindow()
        {
            InitializeComponent();
            this.SetOwnerWindow();
        }


        private readonly AreaRebarMarkFixViewModel viewModel;
        public AreaRebarMarkFixWindow(AreaRebarMarkFixViewModel viewModel) : this()
        {
            this.viewModel = viewModel;
            this.DataContext = viewModel;
            Loaded += Window_Loaded;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.viewModel.RetrieveParameterData();
            Loaded -= Window_Loaded;
        }


        private void Select_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAreaReinElement();
            viewModel.RetrieveParameterData();
        }


        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            viewModel.GetAllAreaReinforceses();
            viewModel.RetrieveParameterData();
        }


        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}
