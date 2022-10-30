using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public readonly Level Level;
        public readonly Element Instanse;
        public ElementModel(Element elem, Level level)
        {
            ElementType etype = elem.Document.GetElement(elem.GetTypeId()) as ElementType;
            if (elem.IsValidObject && level != null && etype.IsValidObject)
            {
                Level = level;
                Instanse = elem;
                LevelName = level.Name;
                SymbolName = etype.Name;
                FamilyName = etype.FamilyName;
            }
        }

        public string LevelName { get; private set; }
        public string SymbolName { get; private set; }
        public string FamilyName { get; private set; }
        public string Description { get; private set; }
        public double Height { get; private set; }
        public double Width { get; private set; }
        public XYZ Origin { get; internal set; }
        public Solid Intersection { get; internal set; }


        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }

        public void SetDescription(double height, double width)
        {
            Height = height; Width = width;
            int w = (int)Math.Round(width * 304.8);
            int h = (int)Math.Round(height * 304.8);
            Description = $" {w}x{h}(h) ".Trim();
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
