using System.Collections.Generic;
using Geek2k16.Entities.Enums;

namespace Geek2k16.Entities.Constants
{
    public static class StringConstants
    {
        public static Dictionary<PieceType, string> PieceNotation = new Dictionary<PieceType, string>
        {
            {PieceType.Rook, "R"},
            {PieceType.Pawn, ""},
            {PieceType.Queen, "Q"},
            {PieceType.King, "K"},
            {PieceType.Knight, "N"},
            {PieceType.Bishop, "B"}
        };

        public static Dictionary<ChessRow, string> RowNotation = new Dictionary<ChessRow, string>
        {
            {ChessRow.Row1, "1"},
            {ChessRow.Row2, "2"},
            {ChessRow.Row3, "3"},
            {ChessRow.Row4, "4"},
            {ChessRow.Row5, "5"},
            {ChessRow.Row6, "6"},
            {ChessRow.Row7, "7"},
            {ChessRow.Row8, "8"}
        };

        public static Dictionary<ChessColumn, string> ColumnNotation = new Dictionary<ChessColumn, string>
        {
            {ChessColumn.A, "a"},
            {ChessColumn.B, "b"},
            {ChessColumn.C, "c"},
            {ChessColumn.D, "d"},
            {ChessColumn.E, "e"},
            {ChessColumn.F, "f"},
            {ChessColumn.G, "g"},
            {ChessColumn.H, "h"}
        };

        public static Dictionary<StateResult, string> ResultNotation = new Dictionary<StateResult, string>
        {
            {StateResult.BlackKingCheckmated, "#  0-1"},
            {StateResult.WhiteKingCheckmated, "#  1-0"},
            {StateResult.BlackKingChecked, "+"},
            {StateResult.WhiteKingChecked, "+"},
            {StateResult.BlackIsOutOfTime, "1-0"},
            {StateResult.BlackIllegalMove, "1-0"},
            {StateResult.WhiteIsOutOfTime, "0-1"},
            {StateResult.WhiteIllegalMove, "0-1"},
            {StateResult.RepeatStateThreeTimes, "½-½"},
            {StateResult.Stalemate, "½-½"},
            {StateResult.FiftyInconsequentialMoves, "½-½"},
            {StateResult.InsufficientMaterial, "½-½"}
        };
    }
}