using Autodesk.Revit.DB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;


namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject, IRevitElementModel
    {
        public readonly int IdInt = 0;
        public readonly int LevelId = 0;
        private readonly Element instance = null;
        private readonly ElementTypeData elemTypeData;
        public ElementModel(Element elem, ElementTypeData data, string description = null)
        {
            instance = elem;
            if (instance.IsValidObject)
            {
                elemTypeData = data;
                IdInt = instance.Id.IntegerValue;
                LevelId = instance.LevelId.IntegerValue;
                CategoryName = instance.Category.Name;
                SymbolName = elemTypeData.SymbolName;
                FamilyName = elemTypeData.FamilyName;
                Description = GetSizeDataToString();
                if (!string.IsNullOrEmpty(description))
                {
                    Description += description;
                }
            }
        }


        public string SymbolName { get; private set; }
        public string FamilyName { get; private set; }
        public string CategoryName { get; private set; }
        public string Description { get; set; } = string.Empty;

        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }


        private string GetSizeDataToString()
        {
            if (elemTypeData.IsValidObject)
            {
                int h = (int)Math.Round(elemTypeData.Height * 304.8);
                int w = (int)Math.Round(elemTypeData.Width * 304.8);
                return $"{w}x{h}(h)".Trim().Normalize();
            }
            return string.Empty;
        }
    }
}
