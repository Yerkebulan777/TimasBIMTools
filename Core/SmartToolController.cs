﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using Revit.Async.Interfaces;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;


namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolController : IExternalApplication
    {
        private UIControlledApplication controller = null;
        public static IServiceProvider Services = CreateServiceProvider();
        public static readonly DockablePaneId DockPaneId = new DockablePaneId(new Guid("{C586E687-A52C-42EE-AC75-CD81EE1E7A9A}"));
        private readonly CutOpeningRegisterDockablePane dockManager = Services.GetRequiredService<CutOpeningRegisterDockablePane>();
        private readonly IDockablePaneProvider provider = Services.GetRequiredService<IDockablePaneProvider>();

        [STAThread]
        public static IServiceProvider CreateServiceProvider()
        {
            IServiceCollection services = new ServiceCollection();

            services = services.AddSingleton<IRevitTask, RevitTask>();
            services = services.AddSingleton<SmartToolGeneralHelper>();
            services = services.AddSingleton<CutOpeningRegisterDockablePane>();

            services = services.AddTransient<IDockablePaneProvider, CutOpeningDockPanelView>();

            services = services.AddSingleton<CutOpeningStartExternalHandler>();
            services = services.AddTransient<CutOpeningCollisionManager>();
            services = services.AddTransient<CutOpeningDataViewModel>();
            services = services.AddTransient<RevitPurginqManager>();

            return services.BuildServiceProvider();
        }


        [STAThread]
        public Result OnStartup(UIControlledApplication cntrapp)
        {
            Logger.InitMainLogger(typeof(SmartToolController));
            SmartToolSetupUIPanel uiface = new SmartToolSetupUIPanel();
            RevitTask.Initialize(cntrapp);
            uiface.Initialize(cntrapp);
            controller = cntrapp;

            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            cntrapp.ControlledApplication.ApplicationInitialized += DockablePaneRegisters;
            cntrapp.ControlledApplication.DocumentClosed += OnDocumentClosed; ;

            return Result.Succeeded;
        }



        [STAThread]
        public Result OnShutdown(UIControlledApplication cntrapp)
        {
            cntrapp.ControlledApplication.ApplicationInitialized -= DockablePaneRegisters;
            cntrapp.ControlledApplication.DocumentClosed -= OnDocumentClosed;
            return Result.Succeeded;
        }


        [STAThread]
        private void DockablePaneRegisters(object sender, ApplicationInitializedEventArgs e)
        {
            if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
            {
                RenderOptions.ProcessRenderMode = RenderMode.Default;
            }
            dockManager.RegisterDockablePane(controller, provider, DockPaneId);
        }


        [STAThread]
        private void OnDocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            try
            {
                DockablePane dockpane = controller.GetDockablePane(DockPaneId);
                if (dockpane.IsShown())
                {
                    dockpane.Hide();
                    dockpane.Dispose();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
