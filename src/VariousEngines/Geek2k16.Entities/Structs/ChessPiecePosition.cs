using Geek2k16.Entities.Enums;
using System;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct ChessPiecePosition
    {
        public ChessPiecePosition(ChessPiece chessPiece, ChessPosition chessPosition)
        {
            ChessPiece = chessPiece;
            ChessPosition = chessPosition;
        }

        public ChessPiecePosition(Player player, PieceType pieceType, ChessColumn column, ChessRow row)
            : this(new ChessPiece(pieceType, player), new ChessPosition(column, row)){}

        public ChessPiece ChessPiece { get; private set; }
        public ChessPosition ChessPosition { get; private set; }

        public override string ToString()
        {
            return $"{ChessPiece}{ChessPosition}";
        }
    }
}