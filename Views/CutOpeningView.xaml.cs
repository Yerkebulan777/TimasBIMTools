using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для CutOpeningView.xaml
    /// </summary>
    public partial class CutOpeningView : UserControl
    {
        public CutOpeningView()
        {
            InitializeComponent();
        }


        private void StartTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(10d);
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Tick -= TimerTick;
            timer.Stop();
        }
    }
}
