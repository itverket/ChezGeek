using ChezGeek.UI.ViewModels;
using Enums = Geek2k16.Entities.Enums;
namespace ChezGeek.UI.UserControls.Converters
{
    public static class PieceTypeConverter
    {
        public static PieceType Map(this Enums.PieceType type)
        {
            switch (type)
            {
                case Enums.PieceType.Bishop:
                    return PieceType.Bishop;
                case Enums.PieceType.King:
                    return PieceType.King;
                case Enums.PieceType.Knight:
                    return PieceType.Knight;
                case Enums.PieceType.Pawn:
                    return PieceType.Pawn;
                case Enums.PieceType.Queen:
                    return PieceType.Queen;
                case Enums.PieceType.Rook:
                    return PieceType.Rook;
                default:
                    return PieceType.Undefined;
            }

        }
    }
}