using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.Services;
using System;
using System.IO;

namespace RevitTimasBIMTools.Core
{
    public class APIEventHandler : IExternalEventHandler
    {
        private readonly string localPath = SmartToolHelper.LocalPath;
        public void Execute(UIApplication app)
        {
            try
            {
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.Reload();
                Document doc = app.ActiveUIDocument.Document;
                Properties.Settings.Default.ActiveDocumentUniqueId = doc.ProjectInformation.UniqueId;
                Properties.Settings.Default.Save();
                if (!Directory.Exists(localPath))
                {
                    DirectoryInfo info = Directory.CreateDirectory(localPath);
                    info.Delete();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + GetName());
            }
        }


        public string GetName()
        {
            return "DockableSample";
        }
    }
}
