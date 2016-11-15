using System.Collections.Generic;
using System.Linq;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamTeal
{
    public class PhasesEvaluator
    {

        public static float EvaluateGeneralMove(ChessMove move)
        {
            float generalBoostScore = 0;
            //prefer to castle
            if (move.PieceType == PieceType.King && move.MoveDistance == 2)
            {
                generalBoostScore += 1;
            }
            //Prefer to not move king if not castle
            if (move.MoveOptions.HasValue && move.MoveOptions.Value == MoveOption.ConvertPawnToQueen)
            {
                generalBoostScore += 10;
            }

            return generalBoostScore * (int)move.Player;
        }


        private static float EvaluateOpeningMoves(ChessMove move, int moveCount)
        {
            float openingBoostScore = 0;
            if (moveCount < 20)
            {

                //Prioritize E4 and D4 
                if (move.PieceType == PieceType.Pawn &&
                    move.MoveDistance == 2)
                {
                    if (move.ChessPiecePosition.ChessPosition.Column == ChessColumn.E)
                    {
                        openingBoostScore += (moveCount < 3 ? 10 : 0.5f);
                    }
                    if (move.ChessPiecePosition.ChessPosition.Column == ChessColumn.D)
                    {
                        openingBoostScore += (moveCount < 3 ? 5 : 0.5f);
                    }
                }

                //Prefer moving un-evolved bishops and Knights
                if ((move.PieceType == PieceType.Bishop || move.PieceType == PieceType.Knight) &&
                    (
                         (move.Player == Player.White && move.ChessPiecePosition.ChessPosition.Row == ChessRow.Row1) ||
                         (move.Player == Player.Black && move.ChessPiecePosition.ChessPosition.Row == ChessRow.Row8)
                     ))
                {
                    if (move.ChessPiecePosition.ChessPosition.Column == ChessColumn.F ||
                        move.ChessPiecePosition.ChessPosition.Column == ChessColumn.G)
                    {
                        openingBoostScore += moveCount < 10 ? 5 : 2;
                    }
                    openingBoostScore += 1;
                }

                //Castle if possible
                if (move.PieceType == PieceType.King && move.MoveDistance == 2)
                {
                    openingBoostScore += 5;
                }
            }

            return openingBoostScore;
        }


        private static float RunAllEvaluations(ChessMove move, int moveCount)
        {
            var openingEvaluation = EvaluateOpeningMoves(move, moveCount);
            var generalEvaluation = EvaluateGeneralMove(move);

            return openingEvaluation + generalEvaluation;
        }

        public static MovePlan[] Boost(MovePlan[] plan, int numberOfMoves)
        {
            return plan.OrderByDescending(x => RunAllEvaluations(x.ChainedMoves.First().NextMove, numberOfMoves)).ToArray();
        }
    }
}