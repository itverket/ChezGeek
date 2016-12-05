using System;
using Geek2k16.Entities.Enums;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct ChessMove
    {
        public ChessMove(ChessPiecePosition chessPiecePosition, ChessPosition toPosition, MoveOption? moveOptions = null)
        {
            ToPosition = toPosition;
            ChessPiecePosition = chessPiecePosition;
            MoveOptions = moveOptions;
        }

        public ChessMove(Player player, PieceType pieceType, ChessColumn fromColumn, ChessRow fromRow,
            ChessColumn toColumn, ChessRow toRow, MoveOption? moveOptions = null)
            : this(new ChessPiecePosition(player, pieceType, fromColumn, fromRow),
                new ChessPosition(toColumn, toRow), moveOptions)
        {
        }

        public ChessMove(ChessPiecePosition chessPiecePosition, ChessColumn toColumn, ChessRow toRow,
            MoveOption? moveOptions = null)
            : this(chessPiecePosition, new ChessPosition(toColumn, toRow), moveOptions)
        {
        }

        public ChessMove(ChessMove chessMove, MoveOption moveOption)
            :this (chessMove.ChessPiecePosition, chessMove.ToPosition, moveOption)
        {
        }

        private int RowDistance => Math.Abs(ChessPiecePosition.ChessPosition.Row - ToPosition.Row);
        private int ColumnDistance => Math.Abs(ChessPiecePosition.ChessPosition.Column - ToPosition.Column);

        public ChessPiecePosition ChessPiecePosition { get; private set; }
        public ChessPosition ToPosition { get; private set; }
        public MoveOption? MoveOptions { get; private set; }
        public PieceType PieceType => ChessPiecePosition.ChessPiece.PieceType;
        public ChessPosition FromPosition => ChessPiecePosition.ChessPosition;
        public int MoveDistance => Math.Max(RowDistance, ColumnDistance);
        public Player Player => ChessPiecePosition.ChessPiece.Player;

        public override string ToString()
        {
            return $"{ChessPiecePosition} -> {ToPosition}";
        }
    }
}