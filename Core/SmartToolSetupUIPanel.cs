using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using RevitTimasBIMTools.CutOpening;
using System;


namespace RevitTimasBIMTools.Core
{
    public sealed class SmartToolSetupUIPanel
    {
        private readonly IServiceProvider provider = SmartToolApp.ServiceProvider;
        private readonly string cutOpeningPane = CutVoidShowPanelCommand.GetPath();
        public void Initialize(UIControlledApplication uicontrol)
        {
            SmartToolHelper helper = provider.GetRequiredService<SmartToolHelper>();
            // Create ribbon tab and ribbon panels
            try { uicontrol.CreateRibbonTab(helper.ApplicationName); } catch { }
            RibbonPanel ribbonPanel = uicontrol.CreateRibbonPanel(helper.ApplicationName, helper.RibbonPanelName);
            PushButtonData buttonData = new("VoidManager", helper.CutVoidToolName, SmartToolHelper.AssemblyLocation, cutOpeningPane)
            {
                ToolTip = "Cut opening purgeMng",
                LargeImage = SmartToolHelper.GetImageSource(),
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