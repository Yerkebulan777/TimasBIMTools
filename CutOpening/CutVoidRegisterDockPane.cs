using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Services;
using System;
using System.Windows;


namespace RevitTimasBIMTools.CutOpening
{
    internal sealed class CutVoidRegisterDockPane
    {
        private readonly IServiceProvider provider = ContainerConfig.ConfigureServices();
        public void RegisterDockablePane(UIControlledApplication uicontrol)
        {
            string cutVoidToolName = SmartToolHelper.CutVoidToolName;
            SmartToolHelper helper = provider.GetRequiredService<SmartToolHelper>();
            IDockablePaneProvider view = provider.GetRequiredService<IDockablePaneProvider>();
            DockablePaneId paneId = helper.CutVoidPaneId;
            if (!DockablePane.PaneIsRegistered(paneId))
            {
                DockablePaneProviderData data = new()
                {
                    FrameworkElement = view as FrameworkElement,
                };
                data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser;
                data.EditorInteraction = new EditorInteraction(EditorInteractionType.Dismiss);
                data.InitialState.DockPosition = DockPosition.Tabbed;
                data.VisibleByDefault = false;
                try
                {
                    uicontrol.RegisterDockablePane(paneId, cutVoidToolName, view);
                }
                catch (Exception exc)
                {
                    Logger.Error($"ERROR:\nguid={paneId.Guid}\n{exc.Message}");
                }
                finally
                {
                    DockablePane dockpane = uicontrol.GetDockablePane(paneId);
                    if (dockpane != null && dockpane.IsShown())
                    {
                        dockpane.Hide();
                    }
                }
            }
        }
    }
}
