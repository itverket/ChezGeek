using System;
using System.Globalization;
using System.Windows.Data;
using ChezGeek.UI.ViewModels;

namespace ChezGeek.UI.UserControls.Converters
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
