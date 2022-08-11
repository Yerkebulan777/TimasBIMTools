using System.Runtime.Serialization;
using Autodesk.Revit.DB;


namespace RevitTimasBIMTools.RevitModel
{
    [DataContract]
    public struct ElementTypeData
    {
        public ElementTypeData(ElementType elemtype, double height = 0, double width = 0, string description = null)
        {
            IsValidObject = elemtype != null && elemtype.IsValidObject;
            Description = description == null ? string.Empty : description;
            if (IsValidObject)
            {
                CategoryIdInt = elemtype.Category.Id.IntegerValue;
                CategoryName = elemtype.Category.Name;
                FamilyName = elemtype.FamilyName;
                SymbolName = elemtype.Name;
            }
            else
            {
                CategoryIdInt = -1;
                CategoryName = string.Empty;
                FamilyName = string.Empty;
                SymbolName = string.Empty;
            }

            Height = height;
            Width = width;
        }

        [DataMember]
        public bool IsValidObject { get; }

        [DataMember]
        public int CategoryIdInt { get; }

        [DataMember]
        public string CategoryName { get; }

        [DataMember]
        public string FamilyName { get; }

        [DataMember]
        public string SymbolName { get; }


        [DataMember]
        public double Height { get; set; }

        [DataMember]
        public double Width { get; set; }

        [IgnoreDataMember]
        public string Description { get; set; }

    }
}
