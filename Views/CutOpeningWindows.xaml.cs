using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

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
            StartCloseTimer(5);
        }

        private void StartCloseTimer(double delay)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(delay);
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Tick -= TimerTick;
            timer.Stop();
            Close();
        }
    }
}
