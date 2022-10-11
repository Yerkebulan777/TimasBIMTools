using Autodesk.Revit.UI;
using RevitTimasBIMTools.CutOpening;

namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolSetupUIPanel
    {
        public static void Initialize(UIControlledApplication uicontrol)
        {
            string cutVoidCommand = CutVoidShowPanelCommand.GetPath();
            // Create ribbon tab and ribbon panels
            try { uicontrol.CreateRibbonTab(SmartToolHelper.ApplicationName); } catch { }
            RibbonPanel ribbonPanel = uicontrol.CreateRibbonPanel(SmartToolHelper.ApplicationName, SmartToolHelper.RibbonPanelName);
            // Create Cut Opening PushButtonData 
            PushButtonData buttonData = new(SmartToolHelper.CutVoidButtonName, SmartToolHelper.CutVoidToolName, SmartToolHelper.AssemblyLocation, cutVoidCommand)
            {
                ToolTip = "Подпись кнопки",
                LargeImage = SmartToolHelper.GetImageSource(),
                LongDescription = "Описание команды кнопки"
            };

            PushButton showButton = ribbonPanel.AddItem(buttonData) as PushButton;
            showButton.AvailabilityClassName = cutVoidCommand;
            if (showButton != null)
            {
                ribbonPanel.AddSeparator();
            }
        }
    }
}