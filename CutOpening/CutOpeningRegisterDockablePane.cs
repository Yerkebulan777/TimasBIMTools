﻿using Autodesk.Revit.UI;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.Services;
using System;
using System.Windows;

namespace RevitTimasBIMTools.CutOpening
{
    internal sealed class CutOpeningRegisterDockablePane
    {
        public void RegisterDockablePane(UIControlledApplication uicontrol, IDockablePaneProvider view, DockablePaneId paneId)
        {
            DockablePane dockpane = null;
            if (!DockablePane.PaneIsRegistered(paneId))
            {
                DockablePaneProviderData data = new DockablePaneProviderData
                {
                    FrameworkElement = view as FrameworkElement,
                };
                data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.PropertiesPalette;
                data.EditorInteraction = new EditorInteraction(EditorInteractionType.Dismiss);
                data.InitialState.DockPosition = DockPosition.Tabbed;
                data.VisibleByDefault = false;
                try
                {
                    uicontrol.RegisterDockablePane(paneId, SmartToolGeneralHelper.CutVoidToolName, view);
                    dockpane = uicontrol.GetDockablePane(paneId);
                }
                catch (Exception exc)
                {
                    Logger.Error($"ERROR:\nguid={paneId.Guid}\n{exc.Message}");
                }
                finally
                {
                    if (dockpane != null && dockpane.IsShown())
                    {
                        dockpane.Hide();
                    }
                }
            }
        }
    }
}
