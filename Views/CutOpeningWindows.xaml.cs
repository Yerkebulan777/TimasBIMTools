using RevitTimasBIMTools.ViewModels;
using System.Windows;

namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для CutOpeningWindows.xaml
    /// </summary>
    public partial class CutOpeningWindows : Window
    {
        public CutOpeningWindows()
        {
            InitializeComponent();
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
