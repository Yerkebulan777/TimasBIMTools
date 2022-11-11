using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using RevitTimasBIMTools.RevitUtils;
using System;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public readonly Level HostLevel;
        public readonly Element Instanse;
        public readonly string SymbolName;
        public readonly string FamilyName;
        public readonly string LevelName;
        public ElementModel(Element elem, Level level)
        {
            ElementType etype = elem.Document.GetElement(elem.GetTypeId()) as ElementType;
            if (elem.IsValidObject && etype.IsValidObject)
            {
                Instanse = elem;
                HostLevel = level;
                LevelName = level.Name;
                SymbolName = etype.Name;
                FamilyName = etype.FamilyName;
            }
        }


        public XYZ Origin { get; internal set; }
        public XYZ Direction { get; internal set; }
        public XYZ HostNormal { get; internal set; }
        public Line IntersectionLine { get; internal set; }
        public Solid IntersectionSolid { get; internal set; }
        public BoundingBoxXYZ IntersectionBox { get; internal set; }
        public string Description { get; internal set; }

        public double Height { get; internal set; }
        public double Width { get; internal set; }


        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        public bool SetSizeDescription()
        {
            if (Width > 0 && Height > 0)
            {
                IsSelected = true;
                int w = (int)Math.Round(Width * 304.8);
                int h = (int)Math.Round(Height * 304.8);
                Description = $"{w}x{h}(h)";
            }
            return IsSelected;
        }


        private void CalculateOpeningSize()
        {
            if (!HostNormal.IsParallel(Direction))
            {
                double horizont = HostNormal.GetHorizontAngleBetween(Direction);
                double vertical =  Direction.GetVerticalAngleByNormal();

            }
        }


        private double CalculateSideSize(double angleRadiance, double hostDeph, double offset)
        {
            return (Math.Tan(angleRadiance) * hostDeph) + (offset * 2);
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
