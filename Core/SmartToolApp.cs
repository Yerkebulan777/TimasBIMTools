using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Revit.Async;
using SmartBIMTools.CutOpening;
using SmartBIMTools.Services;
using System;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;


namespace SmartBIMTools.Core;


public sealed class SmartToolApp : IExternalApplication
{
    public static IHost Host { get; private set; }
    private static SmartToolHelper toolHelper { get; set; }
    private static UIControlledApplication uicontrol { get; set; }
    private static IDockablePaneProvider paneProvider { get; set; }

    private CutHoleRegisterDockPane paneRegister = null;

    public Result OnStartup(UIControlledApplication controlledApp)
    {
        uicontrol = controlledApp;
        Host = ContainerConfig.ConfigureServices();
        SBTLogger.InitMainLogger(typeof(SmartToolApp));
        SmartToolSetupUIPanel.Initialize(controlledApp);
        Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
        controlledApp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
        RevitTask.Initialize(controlledApp);
        return Result.Succeeded;
    }


    [STAThread]
    private void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
    {
        toolHelper = Host.Services.GetRequiredService<SmartToolHelper>();
        paneProvider = Host.Services.GetRequiredService<IDockablePaneProvider>();
        paneRegister = Host.Services.GetRequiredService<CutHoleRegisterDockPane>();
        if (paneRegister.RegisterDockablePane(uicontrol, toolHelper.CutVoidPaneId, paneProvider))
        {
            if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }
        }
    }


    public Result OnShutdown(UIControlledApplication uicontrol)
    {
        uicontrol.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
        return Result.Succeeded;
    }


}
