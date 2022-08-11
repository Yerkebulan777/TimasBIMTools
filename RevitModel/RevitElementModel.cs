using System;
using Autodesk.Revit.DB;
using Microsoft.Toolkit.Mvvm.ComponentModel;


namespace RevitTimasBIMTools.RevitModel
{
    public sealed class RevitElementModel : ObservableObject, IRevitElementModel
    {
        private const double footToMm = 304.8;
        private readonly Element instance = null;
        private readonly ElementTypeData elemTypeData;
        //private readonly string date = DateTime.Today.Date.ToShortDateString();
        public RevitElementModel(Element elem, ElementTypeData data, string description = null)
        {
            instance = elem;
            if (instance.IsValidObject)
            {
                elemTypeData = data;
                IdInt = instance.Id.IntegerValue;
                LevelId = instance.LevelId.IntegerValue;
                SymbolName = elemTypeData.SymbolName;
                FamilyName = elemTypeData.FamilyName;
                CategoryName = instance.Category.Name;
                Description = GetSizeDataToString();
                if (!string.IsNullOrEmpty(description))
                {
                    Description += description;
                }
            }
        }


        public RevitElementModel(FamilySymbol symbol, string description = null)
        {
            instance = symbol;
            if (symbol.IsValidObject)
            {
                SymbolName = symbol.Name.Trim();
                FamilyName = symbol.Family.Name;
                CategoryName = symbol.Category.Name;
                LevelId = symbol.LevelId.IntegerValue;
                IdInt = symbol.Id.IntegerValue;
                if (!string.IsNullOrEmpty(description))
                {
                    Description = description;
                }
            }
        }


        public int IdInt { get; }
        public int LevelId { get; }
        public string SymbolName { get; set; }
        public string FamilyName { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }

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
                int h = (int)Math.Round(elemTypeData.Height * footToMm);
                int w = (int)Math.Round(elemTypeData.Width * footToMm);
                return $"{w}x{h}(h)".Trim().Normalize();
            }
            return string.Empty;
        }
    }
}
