using System;
using System.Globalization;
using System.Windows.Data;


namespace SmartBIMTools.ViewConverters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double size && parameter is string param)
            {
                double.TryParse(param, out double factor);
                return factor * size;
            }
            return (double)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}