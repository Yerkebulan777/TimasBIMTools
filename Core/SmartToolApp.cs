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
        private UIControlledApplication controller { get; set; }
        public static IServiceProvider ServiceProvider { get; private set; }
        public Result OnStartup(UIControlledApplication controlledApp)
        {
            controller = controlledApp;
            RevitTask.Initialize(controlledApp);
            Logger.InitMainLogger(typeof(SmartToolApp));
            SmartToolSetupUIPanel.Initialize(controlledApp);
            ServiceProvider = ContainerConfig.ConfigureServices();
            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            controlledApp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;

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
            register = register ?? throw new ArgumentNullException(nameof(register));
            register.RegisterDockablePane(controller);

        }


        public Result OnShutdown(UIControlledApplication cntrapp)
        {
            cntrapp.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            return Result.Succeeded;
        }


    }
}
