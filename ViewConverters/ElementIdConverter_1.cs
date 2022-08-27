using Autodesk.Revit.DB;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RevitTimasBIMTools.ViewConverters
{
    public class ElementIdConverter : IValueConverter
    {
        private readonly ElementId invalidId = ElementId.InvalidElementId;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is ElementId id)
            {
                return  id != invalidId ? id.IntegerValue : invalidId.IntegerValue;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string val && !string.IsNullOrEmpty(val))
            {
                if (int.TryParse(val, out int id))
                {
                    return new ElementId(id);
                }
            }
            return invalidId;
        }
    }
}
