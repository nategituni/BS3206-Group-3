using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace GroupProject.Model.LogicModel
{
    public class DifficultyToColourConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string difficulty = (value as string)?.ToLower();

            return difficulty switch
            {
                "easy" => Colors.Green,
                "medium" => Colors.Orange,
                "hard" => Colors.Red,
                _ => Colors.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
