using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;


namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public ElementId Id { get; }
        public ElementTypeData TypeData { get; }
        public int HostIntId { get; private set; }
        public int LevelIntId { get; private set; }
        public string SymbolName { get; private set; }
        public string FamilyName { get; private set; }
        public string Description { get; set; } = string.Empty;

        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        public ElementModel(ElementId elementId, ElementTypeData typeData, int hostIntId, int levelIntId, string description)
        {
            Id = elementId;
            TypeData = typeData;
            HostIntId = hostIntId;
            LevelIntId = levelIntId;
            SymbolName = typeData.SymbolName;
            FamilyName = typeData.FamilyName;
            Description = description;
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
