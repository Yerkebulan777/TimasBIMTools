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


        private readonly AreaRebarMarkViewModel viewModel;
        public AreaRebarMarkFixWindow(AreaRebarMarkViewModel vm) : this()
        {
            this.viewModel = vm;
            this.DataContext = vm;
            viewModel.RepresentedView = this;
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
        }


        private void GetAll_Click(object sender, RoutedEventArgs e)
        {
            viewModel.GetAllAreaReinforceses();
        }


        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SetAreaRebarMarkHandler();
            this.Close();
        }


    }
}
