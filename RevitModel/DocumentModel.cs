using Autodesk.Revit.DB;
using System.IO;

namespace RevitTimasBIMTools.RevitModel
{
    public class DocumentModel
    {
        public readonly string Title = null;
        public readonly bool IsActive = false;
        public readonly Document Document = null;
        public readonly string FilePath = string.Empty;
        public readonly Transform Transform = Transform.Identity;
        public DocumentModel(Document document, RevitLinkInstance link = null)
        {
            Document = document;
            FilePath = document.PathName;
            IsActive = document.IsLinked ? false : true;
            Title = Path.GetFileNameWithoutExtension(FilePath).Trim();
            Transform = link != null && document.IsLinked ? link.GetTotalTransform() : Transform.Identity;
        }
    }
}
