using System.Collections.Generic;
using Geek2k16.Entities.Enums;

namespace ChezGeek.TeamYellow.Services
{
    public static class Calculations
    {
        public static Dictionary<PieceType, float> PieceValues = new Dictionary<PieceType, float>
        {
            {PieceType.Pawn, 1f},
            {PieceType.Knight, 3f},
            {PieceType.Bishop, 3f},
            {PieceType.Rook, 6f},
            {PieceType.Queen, 10f},
            {PieceType.King, 1000f }
        };
    }
}