using Autodesk.Revit.DB;
using System;
using System.IO;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class DocumentModel
    {
        public readonly string Title = null;
        public readonly bool IsActive = false;
        public readonly Document Document = null;
        public readonly string FilePath = string.Empty;
        public readonly RevitLinkInstance LinkInstance = null;
        public readonly Transform Transform = Transform.Identity;
        public DocumentModel(Document document, RevitLinkInstance linkInstance = null)
        {
            Document = document;
            LinkInstance = linkInstance;
            FilePath = document.PathName;
            IsActive = document.IsLinked ? false : true;
            Title = Path.GetFileNameWithoutExtension(FilePath).Trim();
            Transform = linkInstance != null && document.IsLinked ? linkInstance.GetTotalTransform() : Transform.Identity;
        }

        public override string ToString()
        {
            return Title;
        }

    }
}
