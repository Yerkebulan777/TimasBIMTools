using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using System;

namespace RevitTimasBIMTools.ViewModels
{
    public class ViewModelLocator
    {
        private static readonly IServiceProvider services = SmartToolController.Services;
        public static CutOpeningDataViewModel DataViewModel => services.GetRequiredService<CutOpeningDataViewModel>();
    }
}
