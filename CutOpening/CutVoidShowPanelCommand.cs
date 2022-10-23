using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Views;
using System;


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
            message = "Error: " + nameof(CutVoidShowPanelCommand);
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
                        CloseDockablePane(pane, view);
                    }
                    else
                    {
                        ShowDockablePane(pane, view);
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


        private async void ShowDockablePane(DockablePane pane, CutVoidDockPaneView view)
        {
            await RevitTask.RunAsync(app =>
            {
                pane?.Show();
                view?.RaiseEvent();
                toolHelper.IsActiveStart = true;
            });
        }


        private async void CloseDockablePane(DockablePane pane, CutVoidDockPaneView view)
        {
            await RevitTask.RunAsync(app =>
            {
                pane?.Hide();
                view?.Dispose();
                toolHelper.IsActiveStart = false;
            });
        }


        [STAThread]
        public bool IsCommandAvailable(UIApplication uiapp, CategorySet categories)
        {
            return uiapp.ActiveUIDocument.Document.IsModifiable == false;
        }


        public static string GetPath()
        {
            return typeof(CutVoidShowPanelCommand).FullName;
        }
    }
}