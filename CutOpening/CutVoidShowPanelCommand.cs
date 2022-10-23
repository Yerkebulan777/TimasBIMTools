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
            toolHelper.IsActiveStart = true;
            DockablePane pane = uiapp.GetDockablePane(toolHelper.CutVoidPaneId);
            if (paneProvider is CutVoidDockPaneView view && pane != null)
            {
                try
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
                catch (Exception ex)
                {
                    message = ex.ToString();
                    return Result.Failed;
                }
                return Result.Succeeded;
            }
            return Result.Failed;
        }


        private async void ShowDockablePane(DockablePane pane, CutVoidDockPaneView view)
        {
            await RevitTask.RunAsync(app =>
            {
                pane.Show();
                view.RaiseEvent();
            });
        }

        private async void CloseDockablePane(DockablePane pane, CutVoidDockPaneView view)
        {
            await RevitTask.RunAsync(app =>
            {
                pane.Hide();
                view.Dispose();
            });
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            return RevitTask.RunAsync(app =>
            {
                return uiapp.ActiveUIDocument.Document.IsModifiable == false;

            }).Result;
        }


        public static string GetPath()
        {
            return typeof(CutVoidShowPanelCommand).FullName;
        }
    }
}