using Autodesk.Revit.UI;
using System;
using System.Text;
using System.Windows;

namespace RevitTimasBIMTools.Core
{
    internal sealed class CutVoidRegisterDockablePane
    {
        public void RegisterDockablePane(UIControlledApplication uicontrol, IDockablePaneProvider view, DockablePaneId paneId)
        {
            StringBuilder builder = new StringBuilder();
            if (!DockablePane.PaneIsRegistered(paneId))
            {
                DockablePaneProviderData data = new DockablePaneProviderData
                {
                    FrameworkElement = view as FrameworkElement,
                };
                data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.PropertiesPalette;
                data.EditorInteraction = new EditorInteraction(EditorInteractionType.KeepAlive);
                data.InitialState.DockPosition = DockPosition.Tabbed;
                data.VisibleByDefault = false;
                try
                {
                    uicontrol.RegisterDockablePane(paneId, SmartToolGeneralHelper.CutVoidToolName, view);
                    builder.AppendLine($"Information:\nDockablePane registered");
                }
                catch (Exception exc)
                {
                    builder.AppendLine($"ERROR:\nguid={paneId.Guid}\n{exc.Message}");
                }
                finally
                {
                    //SendMessageManager.InfoMsg(builder.ToString());
                    builder.Clear();
                }
            }
        }
    }
}
