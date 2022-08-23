using Autodesk.Revit.DB;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitDocumentManager
    {
        public static FilteredElementCollector GetRevitLinkInstanceCollector(Document doc)
        {
            return RevitFilterManager.GetInstancesOfCategory(doc, typeof(RevitLinkInstance), BuiltInCategory.OST_RvtLinks);
        }

        public static IList<RevitDocumenModel> GetDocumentCollection(Document doc)
        {
            List<RevitDocumenModel> documents = new List<RevitDocumenModel>
            {
                new RevitDocumenModel(true, doc, Transform.Identity)
            };
            foreach (RevitLinkInstance link in GetRevitLinkInstanceCollector(doc))
            {
                int id = link.Id.IntegerValue;
                Document linkDocument = link.GetLinkDocument();
                if (linkDocument != null && linkDocument.IsValidObject)
                {
                    try
                    {
                        Transform totalTransform = link.GetTotalTransform();
                        documents.Add(new RevitDocumenModel(false, linkDocument, totalTransform, id));
                    }
                    catch (System.Exception exc)
                    {
                        Logger.Error("linkInstance: " + exc.Message);
                    }
                }
            }
            return documents;
        }
    }
}
