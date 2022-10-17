using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;


namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject, IElementModel
    {
        public int IntId { get; }
        public int LevelIntId { get; }
        public int HostIntId { get; internal set; }
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

        private readonly BoundingBoxXYZ BoundingBox;
        private readonly ElementTypeData SizeTypeData;
        public ElementModel(Element instance, ElementTypeData data, int hostIdInt, BoundingBoxXYZ bbox = null, string description = null)
        {
            if (instance.IsValidObject)
            {
                BoundingBox = bbox;
                SizeTypeData = data;
                HostIntId = hostIdInt;
                IntId = instance.Id.IntegerValue;
                LevelIntId = instance.LevelId.IntegerValue;
                CategoryName = instance.Category.Name;
                SymbolName = SizeTypeData.SymbolName;
                FamilyName = SizeTypeData.FamilyName;
                Description = GetSizeDataToString();
                if (!string.IsNullOrEmpty(description))
                {
                    Description += description;
                }
            }
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }


        private string GetSizeDataToString()
        {
            if (SizeTypeData.IsValidObject)
            {
                int h = (int)Math.Round(SizeTypeData.Height * 304.8);
                int w = (int)Math.Round(SizeTypeData.Width * 304.8);
                return $"{w}x{h}(h)".Normalize();
            }
            return string.Empty;
        }
    }
}
