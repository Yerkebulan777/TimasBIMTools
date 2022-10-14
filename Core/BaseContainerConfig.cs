using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;

namespace RevitTimasBIMTools.Core
{
    public sealed class BaseContainerConfig
    {
        public static IServiceProvider ConfigureServices()
        {
            ServiceCollection services = new();

            _ = services.AddSingleton<SmartToolHelper>();
            _ = services.AddSingleton<CutVoidDataViewModel>();
            _ = services.AddSingleton<CutVoidRegisterDockPane>();
            _ = services.AddSingleton<CutVoidShowPanelCommand>();
            _ = services.AddSingleton<IDockablePaneProvider, CutVoidDockPaneView>();

            _ = services.AddTransient<CutVoidCollisionManager>();
            _ = services.AddTransient<RevitPurginqManager>();

            return services.BuildServiceProvider();
        }
    }
}
