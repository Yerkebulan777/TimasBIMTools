using Autodesk.Revit.DB;
using RevitTimasBIMTools.RevitModel;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitDocumentManager
    {
        public static FilteredElementCollector GetRevitLinkInstanceCollector(Document doc)
        {
            return RevitFilterManager.GetInstancesOfCategory(doc, typeof(RevitLinkInstance), BuiltInCategory.OST_RvtLinks);
        }

        public static IList<DocumentModel> GetDocumentCollection(Document doc)
        {
            List<DocumentModel> docs = new List<DocumentModel> { new DocumentModel(doc) };

            foreach (RevitLinkInstance link in GetRevitLinkInstanceCollector(doc))
            {
                Document linkDoc = link.GetLinkDocument();
                if (linkDoc != null && linkDoc.IsValidObject)
                {
                    docs.Add(new DocumentModel(linkDoc, link));
                }
            }
            return docs;
        }
    }
}
