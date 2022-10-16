using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitTimasBIMTools.Core
{
    internal class APIExternalEventHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            try
            {
                Document doc = app.ActiveUIDocument.Document;
            }
            catch
            {
            }
        }

        public string GetName()
        {
            return "DockableSample";
        }
    }
}
