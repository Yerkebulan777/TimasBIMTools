using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using System;

namespace RevitTimasBIMTools.ViewModels
{
    public static class ViewModelLocator
    {
        private static readonly IServiceProvider services = SmartToolController.Services;
        public static MainViewModel MainViewModel => services.GetRequiredService<MainViewModel>();
        public static CutVoidOpeningViewModel DataViewModel => services.GetRequiredService<CutVoidOpeningViewModel>();

    }
}
