using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public readonly Element Instanse;
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

        public string SymbolName { get; internal set; }
        public string FamilyName { get; internal set; }
        public string Description { get; internal set; }
        public double Height { get; internal set; }
        public double Width { get; internal set; }
        public XYZ Origin { get; internal set; }
        public Level Level { get; internal set; }


        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }

        public void SetDescription(double height, double width, object other = null)
        {
            Height = height; Width = width;
            int w = (int)Math.Round(width * 304.8);
            int h = (int)Math.Round(height * 304.8);
            Description = ($" {w}x{h}(h) " + other.ToString()).Trim();
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
