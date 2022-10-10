using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;


namespace RevitTimasBIMTools.Core
{
    public sealed class ContainerConfig
    {
        public static IServiceProvider ConfigureServices()
        {
            ServiceCollection services = new();

            services.AddSingleton<SmartToolHelper>();
            services.AddSingleton<SmartToolSetupUIPanel>();
            services.AddSingleton<CutVoidRegisterDockPane>();
            services.AddSingleton<CutVoidShowPanelCommand>();

            services.AddSingleton<IDockablePaneProvider, CutVoidDockPanelView>();
            services.AddSingleton<CutVoidDataViewModel>(); //For thread safe

            services.AddTransient<CutVoidViewExternalHandler>();
            services.AddTransient<CutVoidCollisionManager>();
            services.AddTransient<RevitPurginqManager>();
            services.AddTransient<Logger>();

            return services.BuildServiceProvider();
        }
    }
}
