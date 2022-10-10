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
    internal sealed class CutVoidShowPanelCommand : IExternalCommand, IExternalCommandAvailability
    {
        private readonly IServiceProvider provider = SmartToolApp.ServiceProvider;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message);
        }


        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            Result result = Result.Succeeded;
            IDockablePaneProvider pane = provider.GetRequiredService<IDockablePaneProvider>();
            SmartToolHelper helper = provider.GetRequiredService<SmartToolHelper>();
            if (DockablePane.PaneIsRegistered(helper.CutVoidPaneId))
            {
                DockablePane dockpane = uiapp.GetDockablePane(helper.CutVoidPaneId);
                if (pane is CutVoidDockPanelView view)
                {
                    try
                    {
                        if (dockpane.IsShown())
                        {
                            view.Dispose();
                            dockpane.Hide();
                        }
                        else
                        {
                            view.RaiseHandler();
                            dockpane.Show();
                        }
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                        result = Result.Failed;
                    }
                }
            }
            return result;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            return uiapp?.ActiveUIDocument.Document.IsFamilyDocument == false;
        }


        public static string GetPath()
        {
            return typeof(CutVoidShowPanelCommand).FullName;
        }
    }
}