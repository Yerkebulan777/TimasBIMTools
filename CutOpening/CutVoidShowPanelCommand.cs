using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autofac;
using RevitTimasBIMTools.Core;
using System;


namespace RevitTimasBIMTools.CutOpening
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutVoidShowPanelCommand : IExternalCommand, IExternalCommandAvailability
    {
        private readonly SmartToolGeneralHelper helper = SmartToolController.Container.Resolve<SmartToolGeneralHelper>();
        private readonly CutVoidStartExternalHandler handler = SmartToolController.Container.Resolve<CutVoidStartExternalHandler>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message);
        }

        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            Result result = Result.Succeeded;
            DockablePaneId dockid = helper.CutVoidPaneId;
            if (DockablePane.PaneIsRegistered(dockid))
            {
                try
                {
                    ExternalEvent externalEvent = ExternalEvent.Create(handler);
                    DockablePane dockpane = uiapp.GetDockablePane(dockid);
                    if (dockpane.IsShown())
                    {
                        dockpane.Hide();
                    }
                    else
                    {
                        dockpane.Show();
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    result = Result.Failed;
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