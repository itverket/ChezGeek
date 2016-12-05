using Geek2k16.UI.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Geek2k16.UI.UserControls.Converters
{
    public class TileColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return new SolidColorBrush(Colors.White);

            var tileColor = value as TileColor?;

            if (!tileColor.HasValue)
                return new SolidColorBrush(Colors.White);

            return tileColor == TileColor.Light
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
