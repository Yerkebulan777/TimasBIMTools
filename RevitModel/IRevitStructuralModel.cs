using Autodesk.Revit.DB;


namespace RevitTimasBIMTools.RevitModel
{
    internal interface IRevitStructuralModel
    {
        int CategoryId { get; }
        Element Instance { get; }
        Level Level { get; set; }
        Solid Solid { get; set; }
        XYZ Orientation { get; set; }
        BoundingBoxXYZ BoundingBox { get; set; }
    }
}
