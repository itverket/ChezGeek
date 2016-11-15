using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChezGeek.TeamPurple.Openings
{
    abstract internal class OpeningBaseClass : IOpening
    {
        protected Dictionary<int, ChessMove> _openingMoves;

        public OpeningBaseClass()
        {
            _openingMoves = new Dictionary<int, ChessMove>();
            CreateWhiteMoves();
            CreateBlackMoves();
        }

        protected virtual void CreateBlackMoves()
        {
            throw new NotImplementedException();
        }

        protected virtual void CreateWhiteMoves()
        {
            throw new NotImplementedException();
        }

        protected ChessPiecePosition CreateChessPiecePosition(ChessColumn column, ChessRow row, PieceType pieceType, Player player)
        {
            var chessPosition = new ChessPosition(column, row);
            var chessPiece = new ChessPiece(pieceType, player);

            var chessPiecePosition = new ChessPiecePosition(chessPiece, chessPosition);


            return chessPiecePosition;
        }

        protected ChessMove CreateChessMove(ChessPiecePosition chessPiecePosition, ChessColumn columnTo, ChessRow rowTo)
        {
            var chessMove = new ChessMove(chessPiecePosition, columnTo, rowTo);
            return chessMove;
        }

        protected int GetChessMoveStartIndexForPlayer(Player player)
        {
            return player == Player.Black ? 1 : 0;
        }

        ChessMove IOpening.GetNextMove(ChessBoardState chessBoardState)
        {
            var nextMoveIndex = chessBoardState.MoveHistory.Count;
            if (_openingMoves.ContainsKey(nextMoveIndex))
            {
                return _openingMoves[nextMoveIndex];
            }
            return new ChessMove();
        }
    }
}
