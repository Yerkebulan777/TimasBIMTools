using Autodesk.Revit.DB;
using System.IO;

namespace RevitTimasBIMTools.RevitModel
{
    public struct DocumentModel
    {
        public DocumentModel(bool active, Document document, Transform transform, int id = 0)
        {
            Id = id;
            IsActive = active;
            Document = document;
            Transform = transform;
            FilePath = document.PathName;
            Title = Path.GetFileNameWithoutExtension(FilePath).Trim();
        }

        
        public int Id { get; private set; }
        public string Title { get; private set; }
        public bool IsActive { get; private set; }
        public string FilePath { get; private set; }
        public Document Document { get; private set; }
        public Transform Transform { get; private set; }
    }
}
