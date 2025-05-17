using System;
using System.Globalization;
using Microsoft.Maui.Controls;
//placeholder
namespace GroupProject.Model.LogicModel
{
    public class BoolToColourConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool completed = (bool)value;
            return completed ? Colors.Green : Colors.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
