using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;
using System.ComponentModel;

namespace RevitTimasBIMTools.Core
{
    public sealed class ContainerConfig
    {
        public static IServiceProvider ConfigureServices()
        {
            ServiceCollection services = new();

            services.AddSingleton<SmartToolHelper>();
            services.AddSingleton<CutVoidRegisterDockPane>();
            services.AddSingleton<CutVoidShowPanelCommand>();

            services.AddScoped<IDockablePaneProvider, CutVoidDockPaneView>();
            services.AddScoped<INotifyPropertyChanged, CutVoidDataViewModel>();

            services.AddScoped<CutVoidViewExternalHandler>();
            services.AddScoped<CutVoidCollisionManager>();
            services.AddScoped<RevitPurginqManager>();

            return services.BuildServiceProvider();
        }
    }
}
