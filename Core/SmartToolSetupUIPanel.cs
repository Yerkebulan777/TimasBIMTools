using Autodesk.Revit.UI;
using RevitTimasBIMTools.CutOpening;


namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolSetupUIPanel
    {
        public static void Initialize(UIControlledApplication uicontrol)
        {
            string appName = SmartToolHelper.ApplicationName;
            string assemblyName = SmartToolHelper.AssemblyLocation;
            string ribbonPanelName = SmartToolHelper.RibbonPanelName;

            string cutVoidButtonText = SmartToolHelper.CutVoidToolName;
            string cutVoidCommand = CutVoidShowPanelCommand.GetPath();


            // Create ribbon tab and ribbon panels
            uicontrol.CreateRibbonTab(appName);
            RibbonPanel ribbonPanel = uicontrol.CreateRibbonPanel(appName, ribbonPanelName);


            // Create Cut Opening PushButtonData 
            PushButtonData CutVoidbuttonData = new("CutVoidButton", cutVoidButtonText, assemblyName, cutVoidCommand)
            {
                ToolTip = "Cut Openning panel",
                LargeImage = SmartToolHelper.GetImageSource()
            };

            //PushButtonData RebarbuttonData = new("CutVoidButton", cutVoidButtonText, assemblyName, cutVoidCommand)
            //{
            //    ToolTip = "Cut Openning panel",
            //    LargeImage = SmartToolHelper.GetImageSource()
            //};

            PushButton showButton = ribbonPanel.AddItem(CutVoidbuttonData) as PushButton;
            showButton.AvailabilityClassName = cutVoidCommand;
            if (showButton != null)
            {
                ribbonPanel.AddSeparator();
                ///12345
            }
        }
    }
}