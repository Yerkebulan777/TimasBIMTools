using Autodesk.Revit.Attributes;
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
        private DockablePane dockpane = null;
        private readonly DockablePaneId dockpid = SmartToolController.DockPaneId;
        private readonly CutOpeningMainHandler dockpaneHandler = SmartToolController.Services.GetRequiredService<CutOpeningMainHandler>();
        private readonly IDockablePaneProvider provider = SmartToolController.Services.GetRequiredService<IDockablePaneProvider>();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message);
        }

        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            SmartToolController.CurrentDocument = uiapp.ActiveUIDocument.Document;
            ExternalEvent dockpaneExtEvent = ExternalEvent.Create(dockpaneHandler);
            if (dockpid != null && DockablePane.PaneIsRegistered(dockpid))
            {
                dockpane = dockpane ?? uiapp.GetDockablePane(dockpid);
                if (dockpane != null && provider is CutOpeningDockPanelView viewpane)
                {
                    if (dockpane.IsShown())
                    {
                        try
                        {
                            dockpane.Hide();
                            dockpane.Dispose();
                            viewpane.Dispose();
                            dockpaneExtEvent?.Dispose();
                        }
                        catch (Exception exc)
                        {
                            RevitLogger.Error("Show panel error:\t" + exc.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            dockpaneExtEvent?.Raise();
                            dockpane.Show();
                        }
                        catch (Exception exc)
                        {
                            RevitLogger.Error("Show panel error:\t" + exc.Message);
                        }
                    }
                }
            }
            return Result.Succeeded;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            return uidoc != null && !uidoc.Document.IsFamilyDocument;
        }


        public static string GetPath()
        {
            return typeof(CutOpeningShowPanelCmd).FullName;
        }
    }
}