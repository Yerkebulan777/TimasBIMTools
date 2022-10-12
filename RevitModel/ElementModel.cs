using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;


namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject, IRevitElementModel
    {

        private readonly ElementTypeData elemTypeData;
        public ElementModel(Element instance, ElementTypeData data, int hostIdInt = 0, string description = null)
        {
            if (instance.IsValidObject)
            {
                elemTypeData = data;
                HostIdInt = hostIdInt;
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


        public int IdInt { get; }
        public int LevelId { get; }
        public int HostIdInt {get; internal set; }
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
                return $"{w}x{h}(h)".Normalize();
            }
            return string.Empty;
        }
    }
}
