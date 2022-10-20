using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public readonly ElementId Id;
        public readonly ElementType Type;
        public ElementModel(Element element)
        {
            Type = element.Document.GetElement(element.GetTypeId()) as ElementType;
            if (element.IsValidObject && Type != null)
            {
                Id = element.Id;
                SymbolName = Type.Name;
                FamilyName = Type.FamilyName;
            }
            else
            {
                Id = ElementId.InvalidElementId;
            }
        }


        public XYZ Origin { get; internal set; }
        public XYZ HostNormal { get; internal set; }
        public XYZ ModelNormal { get; internal set; }
        public int HostIntId { get; internal set; }
        public int LevelIntId { get; internal set; }
        public string SymbolName { get; internal set; }
        public string FamilyName { get; internal set; }
        public string Description { get; internal set; }


        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        public void SetSizeDescription(double height, double width)
        {
            int h = (int)Math.Round(height * 304.8);
            int w = (int)Math.Round(width * 304.8);
            Description = $"{w}x{h}(h)".Normalize();
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
