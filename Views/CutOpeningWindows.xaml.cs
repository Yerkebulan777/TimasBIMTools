using RevitTimasBIMTools.ViewModels;
using System.Windows;

namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для CutOpeningWindows.xaml
    /// </summary>
    public partial class CutOpeningWindows : Window
    {
        private readonly CutOpeningViewModel openingViewModel = ViewModelLocator.OpeningViewModel;
        public CutOpeningWindows()
        {
            InitializeComponent();
            DataContext = openingViewModel;
        }

        //private void StartCloseTimer(double delay)
        //{
        //    DispatcherTimer timer = new DispatcherTimer();
        //    timer.Interval = TimeSpan.FromMinutes(delay);
        //    timer.Tick += TimerTick;
        //    timer.Start();
        //}

        //private void TimerTick(object sender, EventArgs e)
        //{
        //    DispatcherTimer timer = (DispatcherTimer)sender;
        //    timer.Tick -= TimerTick;
        //    timer.Stop();
        //    Close();
        //}
    }
}
