using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitTimasBIMTools.Core
{
    public class APIEventHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            try
            {
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.Reload();
                Document doc = app.ActiveUIDocument.Document;
                Properties.Settings.Default.ActiveDocumentUniqueId = doc.ProjectInformation.UniqueId;
                Properties.Settings.Default.Save();
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
