using Autodesk.Revit.UI;
using RevitTimasBIMTools.CutOpening;
using System;

namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolSetupUIPanel
    {
        public readonly string AppDirectory = $@"{SmartToolGeneralHelper.AppDataPath}\Autodesk\Revit\Addins\2019\RevitTimasBIMTools";
        private readonly string cutOpeningPane = CutOpeningShowPanelCmd.GetPath();
        private const string buttomText = SmartToolGeneralHelper.CutVoidToolName;
        private const string ribbonPanelName = "Automation";
        private const string buttomName = "BtnVoidManager";

        public void Initialize(UIControlledApplication uicontrol)
        {
            // Create ribbon tab and ribbon panels
            try { uicontrol.CreateRibbonTab(SmartToolGeneralHelper.ApplicationName); } catch { }
            RibbonPanel ribbonPanel = uicontrol.CreateRibbonPanel(SmartToolGeneralHelper.ApplicationName, ribbonPanelName);
            PushButtonData buttonData = new PushButtonData(buttomName, buttomText, SmartToolGeneralHelper.AssemblyLocation, cutOpeningPane)
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