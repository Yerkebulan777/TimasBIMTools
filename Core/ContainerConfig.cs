using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                services.AddSingleton<SmartToolHelper>();
                services.AddSingleton<APIEventHandler>();
                services.AddSingleton<CutVoidDataViewModel>();
                services.AddSingleton<CutHoleRegisterDockPane>();
                services.AddSingleton<CutHoleShowPanelCommand>();

                services.AddSingleton<IDockablePaneProvider, CutVoidDockPaneView>();

                services.AddTransient<CutHoleCollisionManager>();
                services.AddTransient<RevitPurginqManager>();
                services.AddTransient<PreviewControlModel>();
                services.AddTransient<PreviewDialogBox>();

            }).Build();

            return host;
        }
    }
}
