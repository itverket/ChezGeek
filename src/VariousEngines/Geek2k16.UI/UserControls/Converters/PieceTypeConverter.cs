using Geek2k16.UI.ViewModels;

namespace Geek2k16.UI.UserControls.Converters
{
    public static class PieceTypeConverter
    {
        public static PieceType Map(this Entities.Enums.PieceType type)
        {
            switch (type)
            {
                case Entities.Enums.PieceType.Bishop:
                    return PieceType.Bishop;
                case Entities.Enums.PieceType.King:
                    return PieceType.King;
                case Entities.Enums.PieceType.Knight:
                    return PieceType.Knight;
                case Entities.Enums.PieceType.Pawn:
                    return PieceType.Pawn;
                case Entities.Enums.PieceType.Queen:
                    return PieceType.Queen;
                case Entities.Enums.PieceType.Rook:
                    return PieceType.Rook;
                default:
                    return PieceType.Undefined;
            }

        }
    }
}