using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public readonly Level HostLevel;
        public readonly Element Instanse;
        public string LevelName { get; private set; }
        public string SymbolName { get; private set; }
        public string FamilyName { get; private set; }

        public ElementModel(Element elem, Level level)
        {
            Element etype = elem.Document.GetElement(elem.GetTypeId());
            if (etype.IsValidObject && etype is ElementType elementType)
            {
                Instanse = elem;
                HostLevel = level;
                LevelName = level.Name;
                SymbolName = elementType.Name;
                FamilyName = elementType.FamilyName;
            }
        }


        public XYZ Origin { get; internal set; }
        public XYZ Vector { get; internal set; }
        public XYZ HostNormal { get; internal set; }
        public string Description { get; internal set; }


        public double Height { get; internal set; }
        public double Width { get; internal set; }
        public double Depth { get; internal set; }

        public int MinSideSizeValue { get; internal set; }


        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        public bool IsValidModel()
        {
            return Instanse != null && Instanse.IsValidObject && IsSelected;
        }


        public bool IsValidSize(int minSize)
        {
            MinSideSizeValue = Convert.ToInt16(Math.Round(Math.Min(Width, Height) * 304.8));
            if (minSize <= MinSideSizeValue)
            {
                Depth = Math.Abs(HostNormal.DotProduct(Vector));
                int h = Convert.ToInt16(Height * 304.8);
                int w = Convert.ToInt16(Width * 304.8);
                Description = $"{w}x{h}(h)";
                return true;
            }
            return false;
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
