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
            message = "Error: " + nameof(CutVoidShowPanelCommand);
            return Execute(commandData.Application, ref message);
        }


        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            SmartToolHelper toolHelper = provider.GetRequiredService<SmartToolHelper>();
            IDockablePaneProvider paneProvider = provider.GetRequiredService<IDockablePaneProvider>();
            DockablePane dockPane = uiapp.GetDockablePane(toolHelper.CutVoidPaneId);
            if (dockPane != null && dockPane.IsValidObject)
            {
                try
                {
                    if (paneProvider is CutVoidDockPaneView paneView)
                    {
                        if (dockPane.IsShown())
                        {
                            dockPane.Hide();
                        }
                        else
                        {
                            dockPane.Show();
                        }
                    }
                }
                catch (Exception ex)
                {
                    message = ex.ToString();
                    return Result.Failed;
                }
                return Result.Succeeded;
            }
            return Result.Failed;
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