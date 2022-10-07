using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autofac;
using Revit.Async;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.Services;
using System;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolController : IExternalApplication
    {
        public static IContainer Container { get; set; }
        private UIControlledApplication controller { get; set; }


        [STAThread]
        public Result OnStartup(UIControlledApplication cntrapp)
        {
            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            Logger.InitMainLogger(typeof(SmartToolController));
            Container = ContainerConfig.Configure();
            RevitTask.Initialize(cntrapp);
            controller = cntrapp;

            cntrapp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;

            return Result.Succeeded;
        }


        [STAThread]
        private void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }

            CutVoidRegisterDockPane register = Container.Resolve<CutVoidRegisterDockPane>();
            SmartToolGeneralHelper helper = Container.Resolve<SmartToolGeneralHelper>();
            SmartToolSetupUIPanel uiface = Container.Resolve<SmartToolSetupUIPanel>();
            register.RegisterDockablePane(controller);
            uiface.Initialize(controller, helper);
        }


        [STAThread]
        public Result OnShutdown(UIControlledApplication cntrapp)
        {
            cntrapp.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            return Result.Succeeded;
        }

    }
}
