using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public readonly Element Instanse;
        public readonly Level IntersectionLevel;
        public string LevelName { get; private set; }
        public string SymbolName { get; private set; }
        public string FamilyName { get; private set; }


        public ElementModel(Element elem, Level level)
        {
            Element etype = elem.Document.GetElement(elem.GetTypeId());
            if (etype.IsValidObject && etype is ElementType elementType)
            {
                Instanse = elem;
                LevelName = level.Name;
                IntersectionLevel = level;
                SymbolName = elementType.Name;
                FamilyName = elementType.FamilyName;
            }
        }

        public XYZ Origin { get; internal set; }
        public XYZ Direction { get; internal set; }
        public XYZ HostNormal { get; internal set; }
        public string Description { get; internal set; }

        public double Height { get; set; }
        public double Width { get; set; }


        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        public void SetSizeDescription()
        {
            int w = Convert.ToInt16(Width * 304.8);
            int h = Convert.ToInt16(Height * 304.8);
            Description = $"{w}x{h}(h)";
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
