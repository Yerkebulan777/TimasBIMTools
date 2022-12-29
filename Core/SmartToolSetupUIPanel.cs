using Autodesk.Revit.UI;
using RevitTimasBIMTools.Commands;


namespace RevitTimasBIMTools.Core;

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
        PushButtonData cutOpenningBtn = new("CutOpenning", SmartToolHelper.CutOpenningButtonName, assemblyName, CutHoleShowPanelCommand.GetPath())
        {
            ToolTip = "Cut Openning panel",
            LargeImage = SmartToolHelper.GetImageSource()
        };

        PushButtonData areaRebarMarkFixBtn = new("AreaRebarMarkFix", SmartToolHelper.AreaRebarMarkFixButtonName, assemblyName, AreaRebarMarkFixCommand.GetPath())
        {
            ToolTip = "Fix area rebars marks",
            LargeImage = SmartToolHelper.GetImageSource()
        };

        PushButtonData autoJoinButton = new("AutoJoin", SmartToolHelper.AutoJoinButtonName, assemblyName, AutoJoinGeometryCommand.GetPath())
        {
            ToolTip = "Fix area rebars marks",
            LargeImage = SmartToolHelper.GetImageSource()
        };


        //Add buttons to ribbon panel
        PushButton btn01 = ribbonPanel.AddItem(cutOpenningBtn) as PushButton;
        if (btn01 != null && btn01 is PushButton)
        {
            btn01.AvailabilityClassName = CutHoleShowPanelCommand.GetPath();
            ribbonPanel.AddSeparator();
        }

        PushButton btn02 = ribbonPanel.AddItem(areaRebarMarkFixBtn) as PushButton;
        if (btn02 != null && btn02 is PushButton)
        {
            btn02.AvailabilityClassName = AreaRebarMarkFixCommand.GetPath();
            ribbonPanel.AddSeparator();
        }

        PushButton btn03 = ribbonPanel.AddItem(autoJoinButton) as PushButton;
        if (btn03 != null && btn03 is PushButton)
        {
            btn03.AvailabilityClassName = AutoJoinGeometryCommand.GetPath();
            ribbonPanel.AddSeparator();
        }

    }
}