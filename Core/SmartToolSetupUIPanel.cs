using System;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.CutOpening;

namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolSetupUIPanel
    {
        public readonly string AppDirectory = $@"{commonAppData}\Autodesk\Revit\Addins\2019\RevitTimasBIMTools";
        private static readonly string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private readonly string voidPanePath = CutOpeningShowPanelCmd.GetPath();
        private const string buttomText = SmartToolGeneralHelper.CutVoidToolName;
        private const string ribbonPanelName = "Automation";
        private const string buttomName = "BtnVoidManager";


        public void Initialize(UIControlledApplication uicontrol)
        {
            // Create ribbon tab and ribbon panels
            try { uicontrol.CreateRibbonTab(SmartToolGeneralHelper.ApplicationName); } catch { }
            RibbonPanel ribbonPanel = uicontrol.CreateRibbonPanel(SmartToolGeneralHelper.ApplicationName, ribbonPanelName);
            PushButtonData buttonData = new PushButtonData(buttomName, buttomText, SmartToolGeneralHelper.AssemblyLocation, voidPanePath)
            {
                ToolTip = "Cut opening manager",
                LargeImage = SmartToolGeneralHelper.GetImageSource(),
                LongDescription = "Описание команды кнопки"
            };

            PushButton showButton = ribbonPanel.AddItem(buttonData) as PushButton;
            showButton.AvailabilityClassName = voidPanePath;
            if (showButton != null)
            {
                ribbonPanel.AddSeparator();
            }
        }
    }
}