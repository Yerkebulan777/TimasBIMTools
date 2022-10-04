using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using Revit.Async.Interfaces;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolController : IExternalApplication
    {
        public static IServiceProvider Services = CreateServiceProvider();
        private UIControlledApplication controller { get; set; } = null;
        private readonly CutOpeningRegisterDockablePane dockManager = Services.GetRequiredService<CutOpeningRegisterDockablePane>();
        private readonly SmartToolGeneralHelper generalHelper = Services.GetRequiredService<SmartToolGeneralHelper>();
        private readonly IDockablePaneProvider provider = Services.GetRequiredService<IDockablePaneProvider>();

        [STAThread]
        public static IServiceProvider CreateServiceProvider()
        {
            IServiceCollection services = new ServiceCollection();

            services = services.AddSingleton<IRevitTask, RevitTask>();
            services = services.AddSingleton<SmartToolGeneralHelper>();
            services = services.AddSingleton<CutOpeningRegisterDockablePane>();

            services = services.AddScoped<CutOpeningStartExternalHandler>(); /// Try To Scope()

            services = services.AddTransient<IDockablePaneProvider, CutOpeningDockPanelView>();
            services = services.AddTransient<CutOpeningCollisionManager>();
            services = services.AddTransient<CutOpeningDataViewModel>();
            services = services.AddTransient<RevitPurginqManager>();

            return services.BuildServiceProvider();
        }


        [STAThread]
        public Result OnStartup(UIControlledApplication cntrapp)
        {
            Logger.InitMainLogger(typeof(SmartToolController));
            SmartToolSetupUIPanel uiface = new();
            RevitTask.Initialize(cntrapp);
            uiface.Initialize(cntrapp);
            controller = cntrapp;

            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            cntrapp.ControlledApplication.ApplicationInitialized += DockablePaneRegisters;
            cntrapp.Idling += new EventHandler<IdlingEventArgs>(OnIdStart);

            return Result.Succeeded;
        }



        [STAThread]
        public Result OnShutdown(UIControlledApplication cntrapp)
        {
            cntrapp.ControlledApplication.ApplicationInitialized -= DockablePaneRegisters;
            return Result.Succeeded;
        }


        [STAThread]
        private void DockablePaneRegisters(object sender, ApplicationInitializedEventArgs e)
        {
            if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }
            dockManager.RegisterDockablePane(controller, provider, generalHelper.DockPaneId);
        }

        private void OnIdStart(object sender, IdlingEventArgs e)
        {
            controller.Idling -= OnIdStart;
            generalHelper.IsActive = true;
        }

    }
}
