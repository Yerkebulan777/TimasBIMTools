using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Views;
using System;
using System.Linq;

namespace RevitTimasBIMTools.CutOpening
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutVoidShowPanelCommand : IExternalCommand, IExternalCommandAvailability
    {
        private readonly SmartToolHelper toolHelper = SmartToolApp.ServiceProvider.GetRequiredService<SmartToolHelper>();
        private readonly IDockablePaneProvider paneProvider = SmartToolApp.ServiceProvider.GetRequiredService<IDockablePaneProvider>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message);
        }


        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            try
            {
                RevitTask.Initialize(uiapp);
                DockablePane pane = uiapp.GetDockablePane(toolHelper.CutVoidPaneId);
                if (paneProvider is CutVoidDockPaneView view && pane.IsValidObject)
                {
                    if (pane.IsShown())
                    {
                        pane.Hide();
                        view.Dispose();
                        toolHelper.IsActiveStart = false;
                    }
                    else
                    {
                        pane.Show();
                        view.RaiseEvent();
                        toolHelper.IsActiveStart = true;
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


        [STAThread]
        public bool IsCommandAvailable(UIApplication uiapp, CategorySet catSet)
        {
            return RevitFilterManager.GetElementsOfCategory(uiapp.ActiveUIDocument.Document, typeof(Wall), BuiltInCategory.OST_Walls, true).Any();
        }


        public static string GetPath()
        {
            return typeof(CutVoidShowPanelCommand).FullName;
        }
    }
}