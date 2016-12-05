using System.Collections.Generic;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace Geek2k16.Entities.Constants
{
    public static class Converters
    {
        public static readonly Dictionary<Abbr, ChessPiece> ChessPieces = new Dictionary<Abbr, ChessPiece>
        {
            {Abbr.BK, new ChessPiece(PieceType.King, Player.Black) },
            {Abbr.BN, new ChessPiece(PieceType.Knight, Player.Black) },
            {Abbr.BQ, new ChessPiece(PieceType.Queen, Player.Black) },
            {Abbr.BR, new ChessPiece(PieceType.Rook, Player.Black) },
            {Abbr.BB, new ChessPiece(PieceType.Bishop, Player.Black) },
            {Abbr.BP, new ChessPiece(PieceType.Pawn, Player.Black) },
            {Abbr.WK, new ChessPiece(PieceType.King, Player.White) },
            {Abbr.WN, new ChessPiece(PieceType.Knight, Player.White) },
            {Abbr.WQ, new ChessPiece(PieceType.Queen, Player.White) },
            {Abbr.WR, new ChessPiece(PieceType.Rook, Player.White) },
            {Abbr.WB, new ChessPiece(PieceType.Bishop, Player.White) },
            {Abbr.WP, new ChessPiece(PieceType.Pawn, Player.White) },
        };
    }
}