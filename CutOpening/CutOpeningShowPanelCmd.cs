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

        private readonly IServiceProvider provider = SmartToolController.Services;
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
                    using IServiceScope scope = provider.CreateScope();
                    CutOpeningStartExternalHandler dockpaneHandler = scope.ServiceProvider.GetRequiredService<CutOpeningStartExternalHandler>();
                    ExternalEvent dockpaneExtEvent = ExternalEvent.Create(dockpaneHandler);
                    if (dockpane.IsShown())
                    {
                        try
                        {
                            dockpane.Hide();
                            viewpane.Dispose();
                            dockpane.Dispose();
                            dockpaneExtEvent.Dispose();
                        }
                        catch
                        {
                            return Result.Failed;
                        }
                    }
                    else
                    {
                        ExternalEventRequest signal = dockpaneExtEvent.Raise();
                        switch (signal)
                        {
                            case ExternalEventRequest.Accepted:
                                try
                                {
                                    dockpane.Show();
                                }
                                catch
                                {
                                    return Result.Failed;
                                }
                                return Result.Succeeded;
                            default:
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