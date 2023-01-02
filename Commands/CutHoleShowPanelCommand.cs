using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;
using System;
using System.Globalization;
using System.Threading;


namespace RevitTimasBIMTools.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutHoleShowPanelCommand : IExternalCommand, IExternalCommandAvailability
    {
        private readonly SmartToolHelper toolHelper = SmartToolApp.Host.Services.GetRequiredService<SmartToolHelper>();
        private readonly IDockablePaneProvider paneProvider = SmartToolApp.Host.Services.GetRequiredService<IDockablePaneProvider>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            return Execute(commandData.Application, ref message);
        }


        [STAThread]
        public Result Execute(UIApplication uiapp, ref string message)
        {
            Result result = Result.Succeeded;
            try
            {
                DockablePane pane = uiapp.GetDockablePane(toolHelper.CutVoidPaneId);
                if (paneProvider is CutHoleDockPaneView view && pane is not null)
                {
                    if (pane.IsShown())
                    {
                        pane.Hide();
                        view.Dispose();
                    }
                    else
                    {
                        pane.Show();
                        view.RaiseExternalEvent();
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                SBTLogger.Error(message);
                result = Result.Failed;
            }

            return result;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet catSet)
        {
            View view = uiapp.ActiveUIDocument?.ActiveGraphicalView;
            return view is ViewPlan or ViewSchedule or ViewSection or View3D;
        }


        public static string GetPath()
        {
            return typeof(CutHoleShowPanelCommand).FullName;
        }
    }
}