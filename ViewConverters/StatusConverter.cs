using System;
using System.Windows.Data;

namespace RevitTimasBIMTools.ViewConverters
{
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int val;
            if (Int32.TryParse(value.ToString(), out val) && val == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool val;
            if (Boolean.TryParse(value.ToString(), out val) && val == true)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
