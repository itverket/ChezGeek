using System.Collections.Generic;
using System.Linq;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamTeal
{
    public class PieceEvaluator
    {
        public static float EvaluatePawn(IList<ChessPiecePosition> peices, ChessPiecePosition peice)
        {
            float evaluatedValue = 1;
            var connectedPawns = peices.Where(
                x => (
                    x.ChessPiece.Player == peice.ChessPiece.Player &&
                    (x.ChessPosition.Column == peice.ChessPosition.Column + 1 ||
                    x.ChessPosition.Column == peice.ChessPosition.Column - 1)) &&
                     x.ChessPiece.PieceType == PieceType.Pawn).ToList();

            var passedPawn = !peices.Any(
                x => (
                    x.ChessPiece.Player != peice.ChessPiece.Player &&
                    (x.ChessPosition.Column == peice.ChessPosition.Column + 1 ||
                    x.ChessPosition.Column == peice.ChessPosition.Column - 1)) &&
                    (
                        (x.ChessPosition.Row > peice.ChessPosition.Row && peice.ChessPiece.Player == Player.Black) ||
                        (x.ChessPosition.Row < peice.ChessPosition.Row && peice.ChessPiece.Player == Player.White) 
                    )&&
                     x.ChessPiece.PieceType == PieceType.Pawn);

            var doubledPawns = peices.Where(
                x => (
                    x.ChessPiece.Player == peice.ChessPiece.Player &&
                    x.ChessPosition.Column == peice.ChessPosition.Column &&
                    x.ChessPiece.PieceType == PieceType.Pawn)).ToList();

            var blockingPawns = peices.Where(
                x => (
                    x.ChessPiece.Player != peice.ChessPiece.Player &&
                    x.ChessPosition.Column == peice.ChessPosition.Column &&
                    x.ChessPiece.PieceType == PieceType.Pawn)).ToList();


            if (!connectedPawns.Any() || doubledPawns.Any())
            {
                evaluatedValue -= 0.2f;
            }
            if (!blockingPawns.Any())
            {
                evaluatedValue += 0.2f;
            }
            if (!blockingPawns.Any() && passedPawn)
            {
                evaluatedValue += 1;
            }

            return evaluatedValue;
        }

        public static float EvaluateRook(List<ChessPiecePosition> peices, ChessPiecePosition peice)
        {
            float evaluatedValue = 5;
            var inOpenLine = peices.Any(
                x => (
                    x.ChessPiece.Player == peice.ChessPiece.Player &&
                    x.ChessPosition.Column == peice.ChessPosition.Column) &&
                     x.ChessPiece.PieceType == PieceType.Pawn);
            if (inOpenLine)
            {
                evaluatedValue += 0.3f;
            }

            return evaluatedValue;
        }

        public static float EvaluateBishop(List<ChessPiecePosition> peices, ChessPiecePosition peice)
        {
            float evaluatedValue = 3.2f;
            var bishopPair = peices.Count(
                x => (
                    x.ChessPiece.Player == peice.ChessPiece.Player &&
                     x.ChessPiece.PieceType == PieceType.Bishop)) == 2;
            if (bishopPair)
            {
                evaluatedValue += 0.3f;
            }

            return evaluatedValue;
        }
    }
}