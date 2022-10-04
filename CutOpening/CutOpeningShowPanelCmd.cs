﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;
using System;


namespace RevitTimasBIMTools.CutOpening
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutOpeningShowPanelCmd : IExternalCommand, IExternalCommandAvailability
    {
        private readonly SmartToolGeneralHelper generalHelper = SmartToolController.Services.GetRequiredService<SmartToolGeneralHelper>();
        private readonly CutOpeningStartExternalHandler dockpaneHandler = SmartToolController.Services.GetRequiredService<CutOpeningStartExternalHandler>();
        private readonly IDockablePaneProvider provider = SmartToolController.Services.GetRequiredService<IDockablePaneProvider>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message);
        }

        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            DockablePaneId dockpid = generalHelper.DockPaneId;
            if (DockablePane.PaneIsRegistered(dockpid))
            {
                DockablePane dockpane = uiapp.GetDockablePane(dockpid);
                ExternalEvent dockpaneExtEvent = ExternalEvent.Create(dockpaneHandler);
                if (provider is CutOpeningDockPanelView viewpane)
                {
                    if (dockpane.IsShown())
                    {
                        try
                        {
                            dockpane.Hide();
                            viewpane.Dispose();
                            dockpane.Dispose();
                        }
                        catch (Exception exc)
                        {
                            Logger.Error("Show panel error:\t" + exc.Message);
                        }
                        finally
                        {
                            dockpaneExtEvent.Dispose();
                        }
                    }
                    else
                    {
                        try
                        {
                            dockpane.Show();
                        }
                        catch (Exception exc)
                        {
                            Logger.Error("Show panel error:\t" + exc.Message);
                        }
                        finally
                        {
                            _ = dockpaneExtEvent.Raise();
                        }
                    }
                }
            }
            return Result.Succeeded;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            return generalHelper.IsActivated && uiapp?.ActiveUIDocument.Document.IsFamilyDocument == false;
        }


        public static string GetPath()
        {
            return typeof(CutOpeningShowPanelCmd).FullName;
        }
    }
}