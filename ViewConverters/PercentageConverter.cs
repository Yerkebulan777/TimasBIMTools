using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace RevitTimasBIMTools.ViewConverters
{
    public class PercentageConverter : MarkupExtension, IValueConverter
    {
        private static PercentageConverter instance;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return instance ??= new PercentageConverter();
        }
    }
}