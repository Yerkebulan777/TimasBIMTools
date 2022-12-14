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
        private UIControlledApplication controller { get; set; }
        public static IHost Host { get; private set; }
        public Result OnStartup(UIControlledApplication controlledApp)
        {
            controller = controlledApp;
            RevitTask.Initialize(controlledApp);
            Host = ContainerConfig.ConfigureServices();
            Logger.InitMainLogger(typeof(SmartToolApp));
            SmartToolSetupUIPanel.Initialize(controlledApp);
            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            controlledApp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;

            return Result.Succeeded;
        }


        [STAThread]
        private void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            SmartToolHelper toolHelper = Host.Services.GetRequiredService<SmartToolHelper>();
            IDockablePaneProvider paneProvider = Host.Services.GetRequiredService<IDockablePaneProvider>();
            CutVoidRegisterDockPane paneRegister = Host.Services.GetRequiredService<CutVoidRegisterDockPane>();
            toolHelper = toolHelper ?? throw new ArgumentNullException(nameof(toolHelper));
            paneProvider = paneProvider ?? throw new ArgumentNullException(nameof(paneProvider));
            paneRegister = paneRegister ?? throw new ArgumentNullException(nameof(paneRegister));
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
