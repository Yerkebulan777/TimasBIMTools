using Autodesk.Revit.UI;
using Autofac;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;

namespace RevitTimasBIMTools.Core
{
    public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            ContainerBuilder builder = new();

            builder.RegisterType<SmartToolSetupUIPanel>().AsSelf().SingleInstance();
            builder.RegisterType<SmartToolGeneralHelper>().AsSelf().SingleInstance();
            builder.RegisterType<CutVoidRegisterDockPane>().AsSelf().SingleInstance();
            builder.RegisterType<CutVoidDockPanelView>().Named<IDockablePaneProvider>("CutVoidView").SingleInstance();

            builder.RegisterType<CutVoidStartExternalHandler>().AsSelf();
            builder.RegisterType<CutVoidShowPanelCommand>().AsSelf();
            builder.RegisterType<CutVoidCollisionManager>().AsSelf();
            builder.RegisterType<CutVoidDataViewModel>().AsSelf();
            builder.RegisterType<RevitPurginqManager>().AsSelf();

            return builder.Build();
        }

        //Example Autofac automatic registry by assemly: 
        //Assembly dataAccess = Assembly.LoadFrom(nameof(DIContainersLibrary));
        //builder.RegisterAssemblyTypes(dataAccess).Where(t => t.Namespace.Contains("Utils")).AsSelf().AsImplementedInterfaces();

    }
}
