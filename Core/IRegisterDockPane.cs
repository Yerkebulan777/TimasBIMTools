using Autodesk.Revit.UI;

namespace RevitTimasBIMTools.Core
{
    internal interface IRegisterDockPane
    {
        void RegisterDockablePane(UIControlledApplication uicontrol, IDockablePaneProvider view, DockablePaneId paneId);
    }
}