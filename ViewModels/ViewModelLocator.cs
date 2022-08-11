using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using System;

namespace RevitTimasBIMTools.ViewModels
{
    public static class ViewModelLocator
    {
        private static readonly IServiceProvider services = SmartToolController.Services;

        public static MainViewModel MainViewModel => services.GetRequiredService<MainViewModel>();

        public static SettingsViewModel SettingsViewModel => services.GetRequiredService<SettingsViewModel>();

        public static CutOpeningViewModel DataViewModel => services.GetRequiredService<CutOpeningViewModel>();
        
    }
}
