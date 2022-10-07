using Autodesk.Revit.UI;
using Autofac;
using Revit.Async;
using Revit.Async.Interfaces;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;

namespace RevitTimasBIMTools.Core
{
    public static class ContainerConfig
    {
        private static IContainer Configure()
        {
            ContainerBuilder builder = new();

            builder.RegisterType<SmartToolGeneralHelper>().AsSelf().SingleInstance();
            builder.RegisterType<RevitTask>().As<RevitTask, IRevitTask>().SingleInstance();
            builder.RegisterType<CutVoidDockPanelView>().As<IDockablePaneProvider>().SingleInstance();
            builder.RegisterType<CutVoidRegisterDockPane>().As<IRegisterDockPane>().SingleInstance();

            builder.RegisterType<CutOpeningStartExternalHandler>().AsSelf();
            builder.RegisterType<CutOpeningCollisionManager>().AsSelf();
            builder.RegisterType<CutOpeningDataViewModel>().AsSelf();
            builder.RegisterType<RevitPurginqManager>().AsSelf();

            return builder.Build();
        }

        //Example Autofac automatic registry by assemly: 
        //Assembly dataAccess = Assembly.LoadFrom(nameof(DIContainersLibrary));
        //builder.RegisterAssemblyTypes(dataAccess).Where(t => t.Namespace.Contains("Utils")).AsSelf().AsImplementedInterfaces();
    }
}
