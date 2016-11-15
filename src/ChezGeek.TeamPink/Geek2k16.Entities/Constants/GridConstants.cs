using System.Collections.Generic;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using A = Geek2k16.Entities.Enums.Abbr;

namespace Geek2k16.Entities.Constants
{
    public static class GridConstants
    {
        public static readonly A?[,] InitialGrid =
        {
            {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
            {A.BP, A.BP, A.BP, A.BP, A.BP, A.BP, A.BP, A.BP},
            {null, null, null, null, null, null, null, null},
            {null, null, null, null, null, null, null, null},
            {null, null, null, null, null, null, null, null},
            {null, null, null, null, null, null, null, null},
            {A.WP, A.WP, A.WP, A.WP, A.WP, A.WP, A.WP, A.WP},
            {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
        };

        public static class Rules
        {
            public static readonly Dictionary<ChessPiecePosition, StateFlag?> PieceFlags =
                new Dictionary<ChessPiecePosition, StateFlag?>
                {
                    {Positions.WhiteHRook, StateFlag.WhiteHRookHasMoved},
                    {Positions.WhiteARook, StateFlag.WhiteARookHasMoved},
                    {Positions.WhiteKing, StateFlag.WhiteKingHasMoved},
                    {Positions.BlackHRook, StateFlag.BlackHRookHasMoved},
                    {Positions.BlackARook, StateFlag.BlackARookHasMoved},
                    {Positions.BlackKing, StateFlag.BlackKingHasMoved}
                };

            public static readonly Dictionary<ChessMove, ChessMove?> ReactiveMoves =
                new Dictionary<ChessMove, ChessMove?>
                {
                    {Moves.WhiteLongCastling, Moves.WhiteLongCastlingRook},
                    {Moves.WhiteShortCastling, Moves.WhiteShortCastlingRook},
                    {Moves.BlackLongCastling, Moves.BlackLongCastlingRook},
                    {Moves.BlackShortCastling, Moves.BlackShortCastlingRook}
                };

            public static readonly Dictionary<MoveOption, PieceType> PawnPromotion =
                new Dictionary<MoveOption, PieceType>
                {
                    {MoveOption.ConvertPawnToKnight, PieceType.Knight},
                    {MoveOption.ConvertPawnToQueen, PieceType.Queen}
                };

            public static readonly Dictionary<Player, ChessRow> PromotionRows =
                new Dictionary<Player, ChessRow>
                {
                    {Player.White, ChessRow.Row8},
                    {Player.Black, ChessRow.Row1}
                };

            public static readonly Dictionary<Player,ChessRow> KingsRows =
                new Dictionary<Player, ChessRow>
                {
                    {Player.White, ChessRow.Row1 },
                    {Player.Black, ChessRow.Row8 }
                };
        }

        public static class Positions
        {
            public static readonly ChessPiecePosition WhiteKing =
                new ChessPiecePosition(Player.White, PieceType.King, ChessColumn.E, ChessRow.Row1);

            public static readonly ChessPiecePosition WhiteHRook =
                new ChessPiecePosition(Player.White, PieceType.Rook, ChessColumn.H, ChessRow.Row1);

            public static readonly ChessPiecePosition WhiteARook =
                new ChessPiecePosition(Player.White, PieceType.Rook, ChessColumn.A, ChessRow.Row1);

            public static readonly ChessPiecePosition BlackKing =
                new ChessPiecePosition(Player.Black, PieceType.King, ChessColumn.E, ChessRow.Row8);

            public static readonly ChessPiecePosition BlackHRook =
                new ChessPiecePosition(Player.Black, PieceType.Rook, ChessColumn.H, ChessRow.Row8);

            public static readonly ChessPiecePosition BlackARook =
                new ChessPiecePosition(Player.Black, PieceType.Rook, ChessColumn.A, ChessRow.Row8);
        }

        public static class Moves
        {
            public static readonly ChessMove WhiteLongCastling =
                new ChessMove(Positions.WhiteKing, ChessColumn.C, ChessRow.Row1);

            public static readonly ChessMove WhiteLongCastlingRook =
                new ChessMove(Positions.WhiteARook, ChessColumn.D, ChessRow.Row1);

            public static readonly ChessMove WhiteShortCastling =
                new ChessMove(Positions.WhiteKing, ChessColumn.G, ChessRow.Row1);

            public static readonly ChessMove WhiteShortCastlingRook =
                new ChessMove(Positions.WhiteHRook, ChessColumn.F, ChessRow.Row1);

            public static readonly ChessMove BlackLongCastling =
                new ChessMove(Positions.BlackKing, ChessColumn.C, ChessRow.Row8);

            public static readonly ChessMove BlackLongCastlingRook =
                new ChessMove(Positions.BlackARook, ChessColumn.D, ChessRow.Row8);

            public static readonly ChessMove BlackShortCastling =
                new ChessMove(Positions.BlackKing, ChessColumn.G, ChessRow.Row8);

            public static readonly ChessMove BlackShortCastlingRook =
                new ChessMove(Positions.BlackHRook, ChessColumn.F, ChessRow.Row8);
        }
    }
}