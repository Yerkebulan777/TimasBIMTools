using Autodesk.Revit.UI;
using RevitTimasBIMTools.Commands;

namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolSetupUIPanel
    {

        static string appName = SmartToolHelper.ApplicationName;
        static string assemblyName = SmartToolHelper.AssemblyLocation;
        static string ribbonPanelName = SmartToolHelper.RibbonPanelName;

        public static void Initialize(UIControlledApplication uicontrol)
        {
            // Create ribbon tab and ribbon panels
            uicontrol.CreateRibbonTab(appName);
            RibbonPanel ribbonPanel = uicontrol.CreateRibbonPanel(appName, ribbonPanelName);


            // Create Cut Opening PushButtonData 
            PushButtonData CutOpenningButton = new("CutOpenning", SmartToolHelper.CutOpenningButtonName, assemblyName, CutHoleShowPanelCommand.GetPath())
            {
                ToolTip = "Cut Openning panel",
                LargeImage = SmartToolHelper.GetImageSource()
            };

            PushButtonData AreaRebarMarkFixButton = new("AreaRebarMarkFixWindow", SmartToolHelper.AreaRebarMarkFixButtonName, assemblyName, AreaRebarMarkFixCommand.GetPath())
            {
                ToolTip = "Fix area rebar conteiner marks",
                LargeImage = SmartToolHelper.GetImageSource()
            };


            PushButton showButton01 = ribbonPanel.AddItem(CutOpenningButton) as PushButton;
            if (showButton01 != null && showButton01 is PushButton)
            {
                showButton01.AvailabilityClassName = CutHoleShowPanelCommand.GetPath();
                ribbonPanel.AddSeparator();
            }

            PushButton showButton02 = ribbonPanel.AddItem(AreaRebarMarkFixButton) as PushButton;
            if (showButton02 != null && showButton02 is PushButton)
            {
                showButton02.AvailabilityClassName = AreaRebarMarkFixCommand.GetPath();
                ribbonPanel.AddSeparator();
            }

        }
    }
}