using Autofac;
using RevitTimasBIMTools.Core;

namespace RevitTimasBIMTools.ViewModels
{
    public class ViewModelLocator
    {
        private static readonly IContainer provider = SmartToolController.Container;
        public static CutVoidDataViewModel DataViewModel => provider.Resolve<CutVoidDataViewModel>();
    }
}
