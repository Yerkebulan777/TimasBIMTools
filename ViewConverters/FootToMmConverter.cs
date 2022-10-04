using System;
using System.Globalization;
using System.Windows.Data;

namespace RevitTimasBIMTools.ViewConverters
{
    internal class FootToMmConverter : IValueConverter
    {
        private const double footToMm = 12 * 25.4;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double val ? val / footToMm : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double val ? val * footToMm : 0;
        }
    }
}
