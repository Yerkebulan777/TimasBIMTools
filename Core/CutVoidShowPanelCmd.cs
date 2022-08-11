using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;


namespace RevitTimasBIMTools.Core
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutVoidShowPanelCmd : IExternalCommand, IExternalCommandAvailability
    {
        private DockablePane dockpane = null;
        private IExternalEventHandler cashExternalHandler = null;
        private readonly DockablePaneId dockpid = SmartToolController.DockPaneId;
        private readonly IDockablePaneProvider provider = SmartToolController.Services.GetRequiredService<IDockablePaneProvider>();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            cashExternalHandler = SmartToolController.Services.GetRequiredService<IExternalEventHandler>();
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
                            LogManager.Error("Show panel error:\t" + exc.Message);
                        }
                    }
                    else
                    {
                        if (cashExternalHandler is CutVoidBaseCashHandler cashHandler)
                        {
                            try
                            {
                                view.DataHandler = ExternalEvent.Create(cashHandler);
                                view.UpdateContext();
                                dockpane.Show();
                            }
                            catch (System.Exception exc)
                            {
                                LogManager.Error("Show panel error:\t" + exc.Message);
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
            return typeof(CutVoidShowPanelCmd).FullName;
        }
    }
}