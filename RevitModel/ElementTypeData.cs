using System;
using System.Runtime.Serialization;
using Autodesk.Revit.DB;


namespace RevitTimasBIMTools.RevitModel
{
    public struct ElementTypeData
    {
        private const double footToMm = 304.8;
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

            Height = RoundSize(height);
            Width = RoundSize(width);
        }


        public bool IsValidObject { get; }

        public int CategoryIdInt { get; }

        public string CategoryName { get; }

        public string FamilyName { get; }

        public string SymbolName { get; }

        public double Height { get; set; }

        public double Width { get; set; }

        public string Description { get; set; }


        private double RoundSize(double value, int digit = 5)
        {
            return Math.Round(value * footToMm / digit, MidpointRounding.AwayFromZero) * digit / footToMm;
        }

    }
}
