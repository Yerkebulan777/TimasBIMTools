using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public readonly Element Instanse;
        public readonly string SymbolName;
        public readonly string FamilyName;
        public ElementModel(Element elem)
        {
            ElementType etype = elem.Document.GetElement(elem.GetTypeId()) as ElementType;
            if (elem.IsValidObject && etype.IsValidObject)
            {
                Instanse = elem;
                SymbolName = etype.Name;
                FamilyName = etype.FamilyName;
            }
        }

        
        public XYZ Origin { get; internal set; }
        public XYZ Direction { get; internal set; }
        public Level HostLevel { get; internal set; }
        public Solid IntersectionSolid { get; internal set; }
        public BoundingBoxXYZ IntersectionBox { get; internal set; }
        public string Description { get; internal set; }
        public string LevelName { get; internal set; }
        public double Height { get; internal set; }
        public double Width { get; internal set; }


        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        public void SetSizeDescription(double height, double width)
        {
            Height = height; Width = width;
            int w = (int)Math.Round(width * 304.8);
            int h = (int)Math.Round(height * 304.8);
            Description = $"{w}x{h}(h)";
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
