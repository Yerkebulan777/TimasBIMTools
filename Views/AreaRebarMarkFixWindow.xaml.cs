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
            this.viewModel.RetrieveParameterData();
            Loaded -= Window_Loaded;
        }


        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            viewModel.RetrievAreaRebarParameters();
        }


        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}
