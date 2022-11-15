using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Views;
using System;
using System.Globalization;
using System.Threading;

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
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            return Execute(commandData.Application, ref message);
        }


        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            try
            {
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
                        view.RaiseExternalEvent();
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
            return uiapp.ActiveUIDocument.ActiveGraphicalView.ViewType == ViewType.ThreeD;
        }


        public static string GetPath()
        {
            return typeof(CutVoidShowPanelCommand).FullName;
        }
    }
}