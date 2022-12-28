using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RevitTimasBIMTools.RevitUtils
{
    public sealed class RevitContentViewer
    {
        private readonly Document curdoc;
        public Dispatcher dispatch = Dispatcher.CurrentDispatcher;
        public RevitContentViewer(Document curdoc)
        {
            this.curdoc = curdoc;
        }

        public void Show()
        {
            dispatch.Invoke(() =>
            {
                UserControl presenter;
                PreviewControl preview;
                presenter = new UserControl();
                DispatcherTimer dispatcherTimer;
                FilteredElementCollector viewCollector;

                viewCollector = new FilteredElementCollector(curdoc);
                viewCollector.OfClass(typeof(Autodesk.Revit.DB.View));

                //presenter.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                presenter.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                presenter.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                presenter.Height = 500;
                presenter.Width = 500;

                try
                {
                    foreach (View vw in viewCollector)
                    {
                        if (vw.IsValidObject && !vw.IsTemplate && vw is ViewPlan)
                        {
                            preview = new PreviewControl(curdoc, vw.Id as ElementId)
                            {
                                IsManipulationEnabled = true
                            };
                            presenter.Content = preview;
                            break;
                        }
                    }
                }
                catch (Exception exc)
                {
                    Logger.Error(exc.Message);
                }
                finally
                {
                    dispatcherTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMinutes(0.5)
                    };
                    dispatcherTimer.Tick += (s, e) =>
                    {
                        dispatcherTimer.Stop();
                    };
                    dispatcherTimer.Start();
                    viewCollector.Dispose();
                }
            });
        }
    }
}
