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
        private ExternalEvent dockpaneExtEvent { get; set; } = null;
        private CutOpeningStartExternalHandler dockpaneHandler { get; set; } = null;

        private readonly SmartToolGeneralHelper generalHelper = SmartToolController.Services.GetRequiredService<SmartToolGeneralHelper>();
        private readonly IDockablePaneProvider dockProvider = SmartToolController.Services.GetRequiredService<IDockablePaneProvider>();
        private readonly IServiceProvider provider = SmartToolController.Services;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            using IServiceScope scope = provider.CreateScope();
            dockpaneHandler = scope.ServiceProvider.GetRequiredService<CutOpeningStartExternalHandler>();
            dockpaneExtEvent = ExternalEvent.Create(dockpaneHandler);
            return Execute(commandData.Application, ref message);
        }


        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            Result result = Result.Succeeded;
            dockpid = generalHelper.DockPaneId;
            if (DockablePane.PaneIsRegistered(dockpid))
            {
                DockablePane dockpane = uiapp.GetDockablePane(dockpid);
                if (dockpane.IsValidObject && dockProvider is CutOpeningDockPanelView viewpane)
                {
                    viewpane.DockpaneExternalEvent = dockpaneExtEvent;
                    if (viewpane.IsLoaded)
                    {
                        try
                        {
                            dockpane.Hide();
                            viewpane.Dispose();
                        }
                        catch (Exception ex)
                        {
                            message = ex.Message;
                            result = Result.Failed;
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
                            result = Result.Failed;
                        }
                    }
                }
            }
            return result;
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