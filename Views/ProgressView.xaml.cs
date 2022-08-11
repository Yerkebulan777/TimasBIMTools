using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

namespace RevitTimasBIMTools.Views
{
    /// <summary>
    /// Логика взаимодействия для ProgressView.xaml
    /// </summary>
    public partial class ProgressView : Window, IComponentConnector
    {
        internal TextBlock labText;
        internal ProgressBar progressBar;
        internal readonly bool contentLoaded = false;

        public ProgressView()
        {
            InitializeComponent();
        }

        public void SetProgress(string textVal, double i, double maxvalue = double.NaN, bool invokeRequired = false)
        {
            if (invokeRequired)
            {
                Action callback = () =>
                {
                    if (!double.IsNaN(maxvalue))
                    {
                        this.progressBar.Maximum = maxvalue;
                    }

                    if (this.progressBar.Value >= i)
                    {
                        return;
                    }

                    this.labText.Text = textVal;
                    this.progressBar.Value = i;
                };
                Dispatcher.InvokeAsync(callback, DispatcherPriority.Send);
            }
            else
            {
                this.labText.Text = textVal;
                if (!double.IsNaN(maxvalue))
                {
                    this.progressBar.Maximum = maxvalue;
                }

                this.progressBar.Value = i;
            }
            System.Windows.Forms.Application.DoEvents();
        }
    }
}
