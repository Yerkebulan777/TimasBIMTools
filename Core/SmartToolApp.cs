using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        public static IHost Host { get; private set; }
        private UIControlledApplication controller { get; set; }
        public Result OnStartup(UIControlledApplication controlledApp)
        {
            controller = controlledApp;
            Host = ContainerConfig.ConfigureServices();
            Logger.InitMainLogger(typeof(SmartToolApp));
            SmartToolSetupUIPanel.Initialize(controlledApp);
            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            controlledApp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
            RevitTask.Initialize(controlledApp);
            return Result.Succeeded;
        }


        [STAThread]
        private void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            SmartToolHelper toolHelper = Host.Services.GetRequiredService<SmartToolHelper>();
            IDockablePaneProvider paneProvider = Host.Services.GetRequiredService<IDockablePaneProvider>();
            CutHoleRegisterDockPane paneRegister = Host.Services.GetRequiredService<CutHoleRegisterDockPane>();
            if (paneRegister.RegisterDockablePane(controller, toolHelper.CutVoidPaneId, paneProvider))
            {
                if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
                {
                    RenderOptions.ProcessRenderMode = RenderMode.Default;
                }
            }
        }


        public Result OnShutdown(UIControlledApplication cntrapp)
        {
            cntrapp.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            return Result.Succeeded;
        }


    }
}
