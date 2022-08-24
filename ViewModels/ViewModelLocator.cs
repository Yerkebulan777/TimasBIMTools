using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using System;

namespace RevitTimasBIMTools.ViewModels
{
    public static class ViewModelLocator
    {
        private static readonly IServiceProvider services = SmartToolController.Services;

        public static WorkerViewModel MainViewModel => services.GetRequiredService<WorkerViewModel>();

        public static CutOpeningOptionsViewModel OptionsViewModel => services.GetRequiredService<CutOpeningOptionsViewModel>();

        public static CutOpeningDataViewModel DataViewModel => services.GetRequiredService<CutOpeningDataViewModel>();
        
    }
}
