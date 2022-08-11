using Autodesk.RevitAddIns;
using RevitTimasBIMTools.Core;
using System;
using System.IO;

namespace RevitTimasBIMTools.Services
{
    internal static class ManifestFileManager
    {
        public static void GenerateManifestFile()
        {
            const string vendorId = "TIMAS";
            string appName = SmartToolGeneralHelper.AssemblyName;
            string className = typeof(SmartToolController).FullName;
            Guid appGuid = new Guid("GUID");

            RevitAddInManifest manifest = new RevitAddInManifest();
            RevitAddInApplication application = new RevitAddInApplication(appName, appName + ".dll", appGuid, className, vendorId);
            manifest.AddInApplications.Add(application);

            //save manifest to a file
            RevitProduct revitProduct = RevitProductUtility.GetAllInstalledRevitProducts()[0];
            string filePath = Path.Combine(Path.GetFullPath(revitProduct.AllUsersAddInFolder), "TimasBIMTools.addin");
            string localPath = new FileInfo("TimasBIMTools.addin").FullName;
#if DEBUG
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                manifest.SaveAs(filePath);
            }
            manifest.SaveAs(localPath);
#endif
        }
    }
}
