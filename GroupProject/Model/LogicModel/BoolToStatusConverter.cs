using System;
using System.Globalization;
using Microsoft.Maui.Controls;
//placeholder
namespace GroupProject.Model.LogicModel
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool completed)
            {
                return completed ? "Completed" : "Incomplete";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString()?.ToLower() == "completed";
        }
    }
}
