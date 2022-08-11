using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace RevitTimasBIMTools.RevitCommads
{
    public abstract class AutorizedCommand : IExternalCommand
    {
        protected ExternalCommandData CommandData { get; private set; }
        protected UIApplication uiApp { get; private set; }
        protected Document doc { get; private set; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            CommandData = commandData;
            uiApp = CommandData.Application;
            doc = uiApp.ActiveUIDocument.Document;
            try
            {
                /// implementation
            }
            catch (Exception exc)
            {
                message = exc.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }
}
