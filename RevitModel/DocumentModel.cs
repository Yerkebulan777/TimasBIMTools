using Autodesk.Revit.DB;
using System;
using System.IO;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class DocumentModel : IDisposable
    {
        public string Title { get; private set; }
        public bool IsActive { get; private set; }
        public string FilePath { get; private set; }

        public readonly Document Document = null;
        public readonly RevitLinkInstance LinkInstance = null;
        public readonly Transform Transform = Transform.Identity;
        public DocumentModel(Document document, RevitLinkInstance linkInstance = null)
        {
            Document = document;
            LinkInstance = linkInstance;
            FilePath = document.PathName;
            IsActive = !document.IsLinked;
            Title = Path.GetFileNameWithoutExtension(FilePath).ToUpper().Trim();
            Transform = linkInstance != null && document.IsLinked ? linkInstance.GetTotalTransform() : Transform.Identity;
        }

        public void Dispose()
        {
            Title = null;
            FilePath = null;
            Document.Dispose();
            Transform.Dispose();
            LinkInstance.Dispose();
        }

        public override string ToString()
        {
            return Title;
        }

    }
}
