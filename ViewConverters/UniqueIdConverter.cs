using Autodesk.Revit.DB;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RevitTimasBIMTools.ViewConverters
{
    internal class UniqueIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value is Element elem ? elem.UniqueId : (object)null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string val && !string.IsNullOrEmpty(val) ? val : (object)null;
        }
    }
}
