using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using System;

namespace RevitTimasBIMTools.ViewModels
{
    public class ViewModelLocator
    {
        private static readonly IServiceProvider provider = SmartToolApp.ServiceProvider;
        public static CutVoidDataViewModel DataViewModel => provider.GetRequiredService<CutVoidDataViewModel>();
    }
}
