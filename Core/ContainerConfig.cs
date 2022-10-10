using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using Revit.Async.Interfaces;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;


namespace RevitTimasBIMTools.Core
{
    public static class ContainerConfig
    {
        public static IServiceProvider ConfigureServices()
        {
            ServiceCollection services = new();

            services.AddSingleton<SmartToolHelper>();
            services.AddSingleton<IRevitTask, RevitTask>();
            services.AddSingleton<SmartToolSetupUIPanel>();
            services.AddSingleton<CutVoidRegisterDockPane>();
            services.AddSingleton<CutVoidShowPanelCommand>();
            services.AddSingleton<CutVoidViewExternalHandler>();

            services.AddSingleton<IDockablePaneProvider, CutVoidDockPanelView>();
            services.AddSingleton<CutVoidDataViewModel>();

            services.AddTransient<CutVoidCollisionManager>();
            services.AddTransient<RevitPurginqManager>();

            return services.BuildServiceProvider();
        }
    }
}
