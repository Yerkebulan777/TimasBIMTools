using Autodesk.Revit.DB;
using System;


namespace RevitTimasBIMTools.RevitModel
{
    public struct ElementTypeData
    {
        public bool IsValidObject { get; } = false;

        public int CategoryIdInt { get; } = -1;

        public string CategoryName { get; }

        public string FamilyName { get; }

        public string SymbolName { get; }

        public double Height { get; }

        public double Width { get; }

        private const double footToMm = 304.8;


        public ElementTypeData(ElementType elemtype, double height = 0, double width = 0)
        {
            IsValidObject = elemtype != null && elemtype.IsValidObject;
            if (IsValidObject)
            {
                CategoryIdInt = elemtype.Category.Id.IntegerValue;
                CategoryName = elemtype.Category.Name;
                FamilyName = elemtype.FamilyName;
                SymbolName = elemtype.Name;
                Height = RoundSize(height);
                Width = RoundSize(width);
            }
        }


        private double RoundSize(double value, int digit = 5)
        {
            return Math.Round(value * footToMm / digit, MidpointRounding.AwayFromZero) * digit / footToMm;
        }

    }
}
