using Autodesk.Revit.UI;
using Autofac;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Services;
using System;
using System.Windows;


namespace RevitTimasBIMTools.CutOpening
{
    internal sealed class CutVoidRegisterDockPane
    {
        private readonly IContainer container = ContainerConfig.Configure();
        public void RegisterDockablePane(UIControlledApplication uicontrol)
        {
            SmartToolGeneralHelper helper = container.Resolve<SmartToolGeneralHelper>();
            IDockablePaneProvider view = container.ResolveNamed<IDockablePaneProvider>("CutVoidView");
            DockablePaneId paneId = helper.CutVoidPaneId;
            if (!DockablePane.PaneIsRegistered(paneId))
            {
                DockablePaneProviderData data = new()
                {
                    FrameworkElement = view as FrameworkElement,
                };
                data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.PropertiesPalette;
                data.EditorInteraction = new EditorInteraction(EditorInteractionType.Dismiss);
                data.InitialState.DockPosition = DockPosition.Tabbed;
                data.VisibleByDefault = false;
                try
                {
                    uicontrol.RegisterDockablePane(paneId, helper.CutVoidToolName, view);
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
