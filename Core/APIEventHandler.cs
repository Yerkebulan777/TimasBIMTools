using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SmartBIMTools.Services;
using System;
using System.IO;

namespace SmartBIMTools.Core
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
                    Directory.CreateDirectory(localPath);
                }
            }
            catch (Exception ex)
            {
                SBTLogger.Error(ex.Message + GetName());
            }
        }


        public string GetName()
        {
            return "DockableSample";
        }
    }
}
