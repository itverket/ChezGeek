using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geek2k16.Entities;
using Geek2k16.Entities.Structs;
using Geek2k16.Entities.Enums;

namespace ChezGeek.TeamPurple.Openings
{
    class BerlinWallOpening : OpeningBaseClass, IOpening
    {
        protected override void CreateBlackMoves()
        {
            var player = Player.Black;
            var moveIndex = GetChessMoveStartIndexForPlayer(player);

            var chessPiecePosition = CreateChessPiecePosition(ChessColumn.E, ChessRow.Row7, PieceType.Pawn, player);
            _openingMoves.Add(moveIndex, CreateChessMove(chessPiecePosition, ChessColumn.E, ChessRow.Row5));

            moveIndex = moveIndex + 2;
            chessPiecePosition = CreateChessPiecePosition(ChessColumn.B, ChessRow.Row8, PieceType.Knight, player);
            _openingMoves.Add(moveIndex, CreateChessMove(chessPiecePosition, ChessColumn.C, ChessRow.Row6));

            moveIndex = moveIndex + 2;
            chessPiecePosition = CreateChessPiecePosition(ChessColumn.G, ChessRow.Row8, PieceType.Knight, player);
            _openingMoves.Add(moveIndex, CreateChessMove(chessPiecePosition, ChessColumn.F, ChessRow.Row6));
        }

        protected override void CreateWhiteMoves()
        {
            var player = Player.White;

            var moveIndex = GetChessMoveStartIndexForPlayer(player);

            var chessPiecePosition = CreateChessPiecePosition(ChessColumn.E, ChessRow.Row2, PieceType.Pawn, player);
            _openingMoves.Add(moveIndex, CreateChessMove(chessPiecePosition, ChessColumn.E, ChessRow.Row4));

            moveIndex = moveIndex + 2;
            chessPiecePosition = CreateChessPiecePosition(ChessColumn.G, ChessRow.Row1, PieceType.Knight, player);
            _openingMoves.Add(moveIndex, CreateChessMove(chessPiecePosition, ChessColumn.F, ChessRow.Row3));

            moveIndex = moveIndex + 2;
            chessPiecePosition = CreateChessPiecePosition(ChessColumn.F, ChessRow.Row1, PieceType.Bishop, player);
            _openingMoves.Add(moveIndex, CreateChessMove(chessPiecePosition, ChessColumn.B, ChessRow.Row5));
        }
        
    }
}
