using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using System.Windows;


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
            viewModel.GetAllParameterData();
            Loaded -= Window_Loaded;
        }


        private void Select_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAreaReinElement();
            InfoPanel.Visibility = Visibility.Visible;
        }


        private void GetAll_Click(object sender, RoutedEventArgs e)
        {
            viewModel.GetAllAreaReinforceses();
            InfoPanel.Visibility = Visibility.Visible;
        }


        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SetAreaRebarMarkHandler();
            this.Close();
        }


    }
}
