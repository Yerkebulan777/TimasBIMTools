using Autodesk.Revit.DB;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TimasBIMTools.ViewConverters
{
    public class ElementIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ElementId)
            {
                return (value as ElementId).IntegerValue;
            }
            else if (value is int) { return (int)value; }
            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                if (int.TryParse(value as string, out int id))
                {
                    return new ElementId(id);
                }
            }
            return ElementId.InvalidElementId;
        }
    }
}
