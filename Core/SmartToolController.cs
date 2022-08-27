using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using Revit.Async.Interfaces;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Core
{
    public class SmartToolController : IExternalApplication
    {
        private UIControlledApplication controller = null;
        public static Document CurrentDocument { get; set; } = null;
        public static IServiceProvider Services = CreateServiceProvider();
        public static readonly DockablePaneId DockPaneId = new DockablePaneId(new Guid("{C586E687-A52C-42EE-AC75-CD81EE1E7A9A}"));
        private readonly CutOpeningRegisterDockablePane dockManager = Services.GetRequiredService<CutOpeningRegisterDockablePane>();
        private readonly IDockablePaneProvider provider = Services.GetRequiredService<IDockablePaneProvider>();

        [STAThread]
        public static IServiceProvider CreateServiceProvider()
        {
            IServiceCollection services = new ServiceCollection();

            services = services.AddSingleton<IRevitTask, RevitTask>();
            services = services.AddSingleton<CutOpeningMainHandler>();
            services = services.AddSingleton<SmartToolGeneralHelper>();
            services = services.AddSingleton<CutOpeningRegisterDockablePane>();
 
            services = services.AddTransient<IDockablePaneProvider, DockPanelPage>();
            services = services.AddTransient<CutOpeningCollisionManager>();
            services = services.AddTransient<CutOpeningOptionsViewModel>();
            services = services.AddTransient<CutOpeningDataViewModel>();
            services = services.AddTransient<CutOpeningWindows>();
            services = services.AddTransient<SettingsWindow>();

            return services.BuildServiceProvider();
        }


        [STAThread]
        public Result OnStartup(UIControlledApplication cntrapp)
        {
            RevitLogger.InitMainLogger(typeof(SmartToolController));
            SmartToolSetupUIPanel uiface = new SmartToolSetupUIPanel();
            RevitTask.Initialize(cntrapp);
            uiface.Initialize(cntrapp);
            controller = cntrapp;

            Dispatcher.CurrentDispatcher.Thread.Name = "UIRevitGeneralThread";
            cntrapp.ControlledApplication.ApplicationInitialized += DockablePaneRegisters;
            cntrapp.ControlledApplication.DocumentOpened += DocumentOpened;

            return Result.Succeeded;
        }


        [STAThread]
        public Result OnShutdown(UIControlledApplication cntrapp)
        {
            cntrapp.ControlledApplication.ApplicationInitialized -= DockablePaneRegisters;
            cntrapp.ControlledApplication.DocumentOpened -= DocumentOpened;
            Properties.Settings.Default.Reset();
            return Result.Succeeded;
        }


        [STAThread]
        private void DockablePaneRegisters(object sender, ApplicationInitializedEventArgs e)
        {
            if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }
            dockManager.RegisterDockablePane(controller, provider, DockPaneId);
        }


        [STAThread]
        private void DocumentOpened(object sender, DocumentOpenedEventArgs args)
        {
            try
            {
                DockablePane dockpane = controller.GetDockablePane(DockPaneId);
                if (dockpane is DockablePane)
                {
                    if (dockpane.IsShown())
                    {
                        dockpane.Hide();
                        dockpane.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            #region  Is PreviewControl Sample
            //dispatch.Invoke(() =>
            //{
            //    Task.Delay(500);
            //    Window presenter;
            //    PreviewControl prewiew;
            //    presenter = new Window();

            //    Document document = args.Document;

            //    DispatcherTimer dispatcherTimer;

            //    FilteredElementCollector viewCollector = new FilteredElementCollector(document);
            //    viewCollector.OfClass(typeof(Autodesk.Revit.DB.DockPanelView));

            //    presenter.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //    presenter.Height = 500;
            //    presenter.Width = 500;

            //    try
            //    {
            //        foreach (Autodesk.Revit.DB.DockPanelView vw in viewCollector)
            //        {
            //            if (vw.IsValidObject && !vw.IsTemplate && vw is ViewPlan)
            //            {
            //                prewiew = new PreviewControl(document, vw.IdInt as ElementId)
            //                {
            //                    IsManipulationEnabled = true
            //                };
            //                presenter.Content = prewiew;
            //                presenter.Title = vw.SymbolName;
            //                break;
            //            }
            //        }
            //    }
            //    catch (Exception exc)
            //    {
            //        SendMessageManager.ErrorMsg(exc.Message);
            //    }
            //    finally
            //    {
            //        presenter.ShowDialog();
            //        dispatcherTimer = new DispatcherTimer
            //        {
            //            Interval = TimeSpan.FromMinutes(0.5)
            //        };
            //        dispatcherTimer.Tick += (s, e) =>
            //        {
            //            presenter.Close();
            //            dispatcherTimer.Stop();
            //        };
            //        dispatcherTimer.Start();
            //        viewCollector.Dispose();
            //    }
            //});
            #endregion
        }
    }
}
