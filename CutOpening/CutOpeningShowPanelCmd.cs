using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using System;


namespace RevitTimasBIMTools.CutOpening
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutOpeningShowPanelCmd : IExternalCommand, IExternalCommandAvailability
    {
        private DockablePaneId dockid = null;
        private DockablePane dockpane = null;
        private readonly SmartToolGeneralHelper helper = SmartToolController.Services.GetRequiredService<SmartToolGeneralHelper>();
        //private readonly IDockablePaneProvider idockpane = SmartToolController.Services.GetRequiredService<IDockablePaneProvider>();


        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message);
        }


        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            dockid = helper.DockPaneId;
            Result result = Result.Succeeded;
            if (DockablePane.PaneIsRegistered(dockid))
            {
                dockpane = uiapp.GetDockablePane(dockid);
                if (dockpane.IsShown())
                {
                    try
                    {
                        dockpane.Hide();
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
            return result;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            return uiapp?.ActiveUIDocument.Document.IsFamilyDocument == false;
        }


        public static string GetPath()
        {
            return typeof(CutOpeningShowPanelCmd).FullName;
        }
    }
}