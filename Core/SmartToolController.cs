using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autofac;
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
        private static IContainer Container { get; set; }
        public static IServiceProvider Services = CreateServiceProvider();
        private UIControlledApplication controller { get; set; } = null;

        private readonly CutVoidRegisterDockPane register = Services.GetRequiredService<CutVoidRegisterDockPane>();
        private readonly SmartToolGeneralHelper helper = Services.GetRequiredService<SmartToolGeneralHelper>();
        private readonly IDockablePaneProvider provider = Services.GetRequiredService<IDockablePaneProvider>();


        [STAThread]
        public Result OnStartup(UIControlledApplication cntrapp)
        {
            Logger.InitMainLogger(typeof(SmartToolController));
            SmartToolSetupUIPanel uiface = new();
            RevitTask.Initialize(cntrapp);
            uiface.Initialize(cntrapp);
            controller = cntrapp;

            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            cntrapp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;

            return Result.Succeeded;
        }

        [STAThread]
        private void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            register.RegisterDockablePane(controller, provider, helper.DockPaneId);
            if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }
            
            //using (var scope = Container.BeginLifetimeScope())
            //{
            //    var writer = scope.Resolve<IDateWriter>();
            //    writer.WriteDate();
            //}

        }

        [STAThread]
        public Result OnShutdown(UIControlledApplication cntrapp)
        {
            cntrapp.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            return Result.Succeeded;
        }

        public static IServiceProvider CreateServiceProvider()
        {
            IServiceCollection services = new ServiceCollection();

            services = services.AddSingleton<IRevitTask, RevitTask>();
            services = services.AddSingleton<SmartToolGeneralHelper>();
            services = services.AddSingleton<CutVoidRegisterDockPane>();
            services = services.AddScoped<IDockablePaneProvider, CutVoidDockPanelView>();
            services = services.AddTransient<CutOpeningStartExternalHandler>();
            services = services.AddTransient<CutOpeningCollisionManager>();
            services = services.AddTransient<CutOpeningDataViewModel>();
            services = services.AddTransient<RevitPurginqManager>();

            return services.BuildServiceProvider();
        }

        private static IContainer Configure()
        {
            ContainerBuilder builder = new();
            builder.RegisterType<RevitTask>().As<RevitTask, IRevitTask>();
            builder.RegisterType<SmartToolGeneralHelper>().SingleInstance();
            builder.RegisterType<CutVoidRegisterDockPane>().SingleInstance();
            return builder.Build();
        }

    }
}
