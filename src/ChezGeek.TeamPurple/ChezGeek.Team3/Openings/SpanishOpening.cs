using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ChezGeek.TeamPurple.Openings
{
    internal class SpanishOpening : OpeningBaseClass, IOpening
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
