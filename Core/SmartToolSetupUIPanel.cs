using Autodesk.Revit.UI;
using RevitTimasBIMTools.CutOpening;

namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolSetupUIPanel
    {
        private readonly string cutOpeningPane = CutVoidShowPanelCommand.GetPath();
        public void Initialize(UIControlledApplication uicontrol, SmartToolGeneralHelper helper)
        {
            // Create ribbon tab and ribbon panels
            try { uicontrol.CreateRibbonTab(helper.ApplicationName); } catch { }
            RibbonPanel ribbonPanel = uicontrol.CreateRibbonPanel(helper.ApplicationName, helper.RibbonPanelName);
            PushButtonData buttonData = new("VoidManager", helper.CutVoidToolName, SmartToolGeneralHelper.AssemblyLocation, cutOpeningPane)
            {
                ToolTip = "Cut opening purgeMng",
                LargeImage = SmartToolGeneralHelper.GetImageSource(),
                LongDescription = "Описание команды кнопки"
            };

            PushButton showButton = ribbonPanel.AddItem(buttonData) as PushButton;
            showButton.AvailabilityClassName = cutOpeningPane;
            if (showButton != null)
            {
                ribbonPanel.AddSeparator();
            }
        }
    }
}