using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Revit.Async;
using SmartBIMTools.Commands;
using SmartBIMTools.CutOpening;
using SmartBIMTools.RevitModel;
using SmartBIMTools.RevitUtils;
using SmartBIMTools.ViewModels;
using SmartBIMTools.Views;


namespace SmartBIMTools.Core
{
    public sealed class ContainerConfig
    {
        public static IHost ConfigureServices()
        {
            IHost host = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                // Singleton
                services.AddSingleton<RevitTask>();
                services.AddSingleton<SmartToolHelper>();
                services.AddSingleton<IDockablePaneProvider, CutHoleDockPaneView>();

                // CutOpenningManager
                services.AddTransient<APIEventHandler>();
                services.AddTransient<CutHoleDataViewModel>();
                services.AddTransient<CutHoleRegisterDockPane>();
                services.AddTransient<CutHoleShowPanelCommand>();
                services.AddTransient<CutHoleCollisionManager>();
                services.AddTransient<RevitPurginqManager>();
                services.AddTransient<PreviewControlModel>();
                services.AddTransient<PreviewDialogBox>();

                // RoomFinishing
                services.AddTransient<FinishingViewModel>();
                services.AddTransient<RoomFinishingWindow>();

                // AreaRebarMarkFix
                services.AddTransient<AreaRebarMarkViewModel>();
                services.AddTransient<AreaRebarMarkFixWindow>();

            }).Build();

            return host;
        }
    }
}
