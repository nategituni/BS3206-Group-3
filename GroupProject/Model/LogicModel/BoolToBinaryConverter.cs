using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace GroupProject.Model.LogicModel
{
    public class BoolToBinaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "1" : "0";
            if (value is string s && bool.TryParse(s, out bool result))
                return result ? "1" : "0";
            return "0";
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == "1";
        }
    }
}

