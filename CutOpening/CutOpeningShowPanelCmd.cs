using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;


namespace RevitTimasBIMTools.CutOpening
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutOpeningShowPanelCmd : IExternalCommand, IExternalCommandAvailability
    {
        private DockablePane dockpane = null;
        private readonly DockablePaneId dockpid = SmartToolController.DockPaneId;
        private IExternalEventHandler externalHandler = SmartToolController.Services.GetRequiredService<CutOpeningMainHandler>();
        private readonly IDockablePaneProvider provider = SmartToolController.Services.GetRequiredService<IDockablePaneProvider>();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message);
        }

        public Result Execute(UIApplication uiapp, ref string message)
        {
            if (DockablePane.PaneIsRegistered(dockpid))
            {
                dockpane = dockpane ?? uiapp.GetDockablePane(dockpid);
                if (dockpane != null && provider is DockPanelPage view)
                {
                    if (dockpane.IsShown())
                    {
                        try
                        {
                            view?.Dispose();
                            dockpane.Hide();
                            dockpane.Dispose();
                        }
                        catch (System.Exception exc)
                        {
                            Logger.Error("Show panel error:\t" + exc.Message);
                        }
                    }
                    else
                    {
                        if (externalHandler is CutOpeningMainHandler handler)
                        {
                            try
                            {
                                view.ViewExternalEvent = ExternalEvent.Create(handler);
                                view.UpdateContext();
                                dockpane.Show();
                            }
                            catch (System.Exception exc)
                            {
                                Logger.Error("Show panel error:\t" + exc.Message);
                            }
                        }
                    }
                }
            }
            return Result.Succeeded;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc != null)
            {
                return !uidoc.Document.IsFamilyDocument;
            }
            return false;
        }

        public static string GetPath()
        {
            return typeof(CutOpeningShowPanelCmd).FullName;
        }
    }
}