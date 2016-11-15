using System.Collections.Generic;
using Geek2k16.Entities.Enums;

namespace Geek2k16.Entities.Constants
{
    public static class Calculations
    {
        public static Dictionary<PieceType, float> PieceValues = new Dictionary<PieceType, float>
        {
            {PieceType.Pawn, 1f},
            {PieceType.Knight, 3f},
            {PieceType.Bishop, 3f},
            {PieceType.Rook, 5f},
            {PieceType.Queen, 9f},
            {PieceType.King, 1000f }
        };
    }
}