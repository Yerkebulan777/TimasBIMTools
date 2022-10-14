﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Views;
using System;


namespace RevitTimasBIMTools.CutOpening
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutVoidShowPanelCommand : IExternalCommand, IExternalCommandAvailability
    {
        private static readonly IServiceProvider provider = SmartToolApp.ServiceProvider;
        private SmartToolHelper toolHelper { get; set; } = provider.GetRequiredService<SmartToolHelper>();
        private IDockablePaneProvider paneProvider { get; set; } = provider.GetRequiredService<IDockablePaneProvider>();
        private CutVoidRegisterDockPane paneRegister { get; set; } = provider.GetRequiredService<CutVoidRegisterDockPane>();


        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            toolHelper = toolHelper ?? throw new ArgumentNullException(nameof(toolHelper));
            paneProvider = paneProvider ?? throw new ArgumentNullException(nameof(paneProvider));
            paneRegister = paneRegister ?? throw new ArgumentNullException(nameof(paneRegister));
            return Execute(commandData.Application, ref message);
        }


        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            DockablePaneId paneId = toolHelper.CutVoidPaneId;
            if (paneRegister.RegisterDockablePane(uiapp, paneId, paneProvider))
            {
                DockablePane dockPane = uiapp.GetDockablePane(paneId);
                if (dockPane != null && dockPane.IsValidObject)
                {
                    try
                    {
                        if (paneProvider is CutVoidDockPaneView paneView)
                        {
                            if (dockPane.IsShown())
                            {
                                dockPane.Hide();
                                paneView.Dispose();
                            }
                            else
                            {
                                dockPane.Show();
                                paneView.RaiseHandler();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                        return Result.Failed;
                    }
                }
            }
            return Result.Succeeded;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            return uiapp.ActiveUIDocument.IsValidObject && uiapp.ActiveUIDocument.Document.IsFamilyDocument == false;
        }


        public static string GetPath()
        {
            return typeof(CutVoidShowPanelCommand).FullName;
        }
    }
}