using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Revit.Async;
using RevitTimasBIMTools.Commands;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;


namespace RevitTimasBIMTools.Core
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
                services.AddSingleton<IDockablePaneProvider, CutVoidDockPaneView>();

                // CutOpenningManager
                services.AddTransient<APIEventHandler>();
                services.AddTransient<CutHoleDataViewModel>();
                services.AddTransient<CutHoleRegisterDockPane>();
                services.AddTransient<CutHoleShowPanelCommand>();
                services.AddTransient<CutHoleCollisionManager>();
                services.AddTransient<RevitPurginqManager>();
                services.AddTransient<PreviewControlModel>();
                services.AddTransient<PreviewDialogBox>();

                // AreaRebarMarkFix
                services.AddTransient<AreaRebarMarkViewModel>();
                services.AddTransient<AreaRebarMarkFixWindow>();

            }).Build();

            return host;
        }
    }
}
