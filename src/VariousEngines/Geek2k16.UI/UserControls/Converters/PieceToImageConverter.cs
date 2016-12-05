using Geek2k16.UI.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Geek2k16.UI.UserControls.Converters
{
    public class PieceToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var piece = value as Piece;

            if (piece == null)
                return string.Empty;

            return $"/Resources/Images/{piece.PieceType.ToString().ToLower()}_{piece.PieceColor.ToString().ToLower()}.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
