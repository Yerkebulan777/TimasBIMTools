using Autodesk.Revit.UI;
using RevitTimasBIMTools.Commands;


namespace RevitTimasBIMTools.Core;

public sealed class SmartToolSetupUIPanel
{
    private static readonly string appName = SmartToolHelper.ApplicationName;
    private static readonly string assemblyName = SmartToolHelper.AssemblyLocation;
    private static readonly string ribbonPanelName = SmartToolHelper.RibbonPanelName;

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


        PushButtonData areaRebarMarkFixBtn = new("AreaRebarMark", SmartToolHelper.AreaRebarMarkButtonName, assemblyName, AreaRebarMarkFixCommand.GetPath())
        {
            ToolTip = "Fix area rebars marks",
            LargeImage = SmartToolHelper.GetImageSource()
        };


        PushButtonData autoJoinButton = new("AutoJoin", SmartToolHelper.AutoJoinButtonName, assemblyName, AutoJoinGeometryCommand.GetPath())
        {
            ToolTip = "Fix area rebars marks",
            LargeImage = SmartToolHelper.GetImageSource()
        };


        PushButtonData finishingButton = new("Finishing", SmartToolHelper.FinishingButtonName, assemblyName, RoomFinishingCommand.GetPath())
        {
            ToolTip = "Instance finishing command",
            LargeImage = SmartToolHelper.GetImageSource()
        };


        //Add buttons to ribbon panel
        if (ribbonPanel.AddItem(cutOpenningBtn) is PushButton btn01)
        {
            btn01.AvailabilityClassName = CutHoleShowPanelCommand.GetPath();
            ribbonPanel.AddSeparator();
        }

        if (ribbonPanel.AddItem(areaRebarMarkFixBtn) is PushButton btn02)
        {
            btn02.AvailabilityClassName = AreaRebarMarkFixCommand.GetPath();
            ribbonPanel.AddSeparator();
        }

        if (ribbonPanel.AddItem(autoJoinButton) is PushButton btn03)
        {
            btn03.AvailabilityClassName = AutoJoinGeometryCommand.GetPath();
            ribbonPanel.AddSeparator();
        }

        if (ribbonPanel.AddItem(finishingButton) is PushButton btn04)
        {
            btn04.AvailabilityClassName = RoomFinishingCommand.GetPath();
            ribbonPanel.AddSeparator();
        }

    }
}