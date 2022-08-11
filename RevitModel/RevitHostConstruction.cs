using System;

using Autodesk.Revit.DB;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class RevitHostConstruction : IRevitStructuralModel, IDisposable
    {
        private bool disposed = false;
        public RevitHostConstruction(int catIdValue, Element elem, BoundingBoxXYZ bbox, Solid solid, XYZ orientation)
        {
            Orientation = orientation;
            CategoryId = catIdValue;
            BoundingBox = bbox;
            Instance = elem;
            Solid = solid;
        }

        public int CategoryId { get; }
        public Element Instance { get; }
        public Level Level { get; set; }
        public Solid Solid { get; set; }
        public XYZ Orientation { get; set; }
        public BoundingBoxXYZ BoundingBox { get; set; }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Orientation = XYZ.Zero;
                BoundingBox.Dispose();
                Instance.Dispose();
                Solid.Dispose();
            }
        }
    }
}
