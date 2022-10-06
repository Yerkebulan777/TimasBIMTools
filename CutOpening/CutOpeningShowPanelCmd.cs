using Autodesk.Revit.Attributes;
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
    internal sealed class CutOpeningShowPanelCmd : IExternalCommand, IExternalCommandAvailability
    {
        private DockablePaneId dockpid { get; set; } = null;
        private readonly SmartToolGeneralHelper generalHelper = SmartToolController.Services.GetRequiredService<SmartToolGeneralHelper>();
        private readonly IDockablePaneProvider dockProvider = SmartToolController.Services.GetRequiredService<IDockablePaneProvider>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message);
        }

        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            dockpid = generalHelper.DockPaneId;
            if (DockablePane.PaneIsRegistered(dockpid))
            {
                DockablePane dockpane = uiapp.GetDockablePane(dockpid);
                if (dockProvider is CutOpeningDockPanelView viewpane)
                {
                    if (dockpane.IsShown())
                    {
                        try
                        {
                            dockpane.Hide();
                            viewpane.Dispose();
                            dockpane.Dispose();
                        }
                        catch (Exception ex)
                        {
                            message = ex.Message;
                            return Result.Failed;
                        }
                    }
                    else
                    {
                        try
                        {
                            dockpane.Show();
                        }
                        catch (Exception ex)
                        {
                            message = ex.Message;
                            return Result.Failed;
                        }
                    }
                }
            }
            return Result.Succeeded;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            return generalHelper.IsActive && uiapp?.ActiveUIDocument.Document.IsFamilyDocument == false;
        }


        public static string GetPath()
        {
            return typeof(CutOpeningShowPanelCmd).FullName;
        }
    }
}