using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.Services;
using System;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolApp : IExternalApplication
    {
        public static IServiceProvider ServiceProvider { get; set; }
        private UIControlledApplication controller { get; set; }


        [STAThread]
        public Result OnStartup(UIControlledApplication cntrapp)
        {
            ServiceProvider = ContainerConfig.ConfigureServices();
            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            cntrapp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
            
            controller = cntrapp;

            return Result.Succeeded;
        }


        [STAThread]
        private void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }

            CutVoidRegisterDockPane register = ServiceProvider.GetRequiredService<CutVoidRegisterDockPane>();
            SmartToolSetupUIPanel uiface = ServiceProvider.GetRequiredService<SmartToolSetupUIPanel>();
            Logger.InitMainLogger(typeof(SmartToolApp));
            register.RegisterDockablePane(controller);
            RevitTask.Initialize(controller);
            uiface.Initialize(controller);
            
        }


        [STAThread]
        public Result OnShutdown(UIControlledApplication cntrapp)
        {
            cntrapp.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            return Result.Succeeded;
        }

    }
}
