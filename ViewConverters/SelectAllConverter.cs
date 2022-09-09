using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RevitTimasBIMTools.ViewConverters
{
    internal class SelectAllConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int amount = (values[0] != null && values[0] != DependencyProperty.UnsetValue) ? System.Convert.ToInt16(values[0]) : 0;
            bool flag = (values[1] != null && values[1] != DependencyProperty.UnsetValue && values[1] is true) ? false : true;
            return (flag && amount > 0);
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
