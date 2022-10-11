using Autodesk.Revit.UI;


namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolSetupUIPanel
    {
        public static void Initialize(UIControlledApplication uicontrol)
        {
            string appName = SmartToolHelper.ApplicationName;
            string appPath = SmartToolHelper.AssemblyLocation;
            string panelName = SmartToolHelper.RibbonPanelName;
            string cutVoidToolName = SmartToolHelper.CutVoidToolName;
            string cutVoidCmdPath = SmartToolHelper.CutVoidToolName;
            // Create ribbon tab and ribbon panels
            try { uicontrol.CreateRibbonTab(appName); } catch { }
            RibbonPanel ribbonPanel = uicontrol.CreateRibbonPanel(appName, panelName);
            PushButtonData buttonData = new("VoidManager", cutVoidToolName, appPath, cutVoidCmdPath)
            {
                ToolTip = "Cut opening purgeMng",
                LargeImage = SmartToolHelper.GetImageSource(),
                LongDescription = "Описание команды кнопки"
            };

            PushButton showButton = ribbonPanel.AddItem(buttonData) as PushButton;
            showButton.AvailabilityClassName = cutVoidCmdPath;
            if (showButton != null)
            {
                ribbonPanel.AddSeparator();
            }
        }
    }
}