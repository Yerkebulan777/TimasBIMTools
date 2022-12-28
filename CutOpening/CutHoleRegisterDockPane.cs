using Autodesk.Revit.UI;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Services;
using System;
using System.Windows;


namespace RevitTimasBIMTools.CutOpening
{
    internal sealed class CutHoleRegisterDockPane
    {
        private readonly string cutHoleToolName = SmartToolHelper.CutOpenningButtonName;
        public bool RegisterDockablePane(UIControlledApplication controller, DockablePaneId paneId, IDockablePaneProvider dockPane)
        {
            if (!DockablePane.PaneIsRegistered(paneId))
            {
                DockablePaneProviderData data = new()
                {
                    FrameworkElement = dockPane as FrameworkElement,
                };
                data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser;
                data.EditorInteraction = new EditorInteraction(EditorInteractionType.KeepAlive);
                data.InitialState.DockPosition = DockPosition.Right;
                data.VisibleByDefault = false;
                try
                {
                    controller.RegisterDockablePane(paneId, cutHoleToolName, dockPane);
                }
                catch (Exception exc)
                {
                    SBTLogger.Error($"ERROR:\nguid={paneId.Guid}\n{exc.Message}");
                    return false;
                }
            }
            return true;
        }
    }
}
