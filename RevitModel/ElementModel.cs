using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;


namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public ElementId Id { get; }
        public ElementTypeData TypeData { get; }

        public ElementModel(ElementId elementId, ElementTypeData typeData)
        {
            Id = elementId;
            TypeData = typeData;
        }

        public XYZ Normal { get; internal set; }
        public Outline Outline { get; internal set; }
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


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
