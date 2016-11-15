using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geek2k16.Entities;
using Geek2k16.Entities.Constants;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace ChezGeek.TeamPurple.Services
{
    public class PurpleCalculationsService
    {
        private readonly ChessCalculationsService _chessCalculationsService;

        public PurpleCalculationsService(ChessCalculationsService chessCalculationsService)
        {
            _chessCalculationsService = chessCalculationsService;
        }

        public MovePlan[] GetBestPlans(AvailableStateMoves thisPlyStateMoves, Dictionary<PreliminaryStateAndMove, MovePlan> thisPlyToNextPlyEvaluations)
        {
            var state = thisPlyStateMoves.State;
            var nextToMove = state.NextToMove;
            return thisPlyStateMoves.NextMoves
                .Select(m => new PreliminaryStateAndMove(state, m))
                .Select(m => CreateMovePlan(m, thisPlyToNextPlyEvaluations[m]))
                .GroupBy(y => y.EstimatedValue * (int)nextToMove)
                .OrderByDescending(x => x.Key)
                .First().ToArray();
        }
        public MovePlan[] GetBestPlans(AvailableStateMoves leafStateMoves)
        {
            var state = leafStateMoves.State;
            var nextToMove = state.NextToMove;
            return leafStateMoves.NextMoves
                .Select(m => new PreliminaryStateAndMove(state, m))
                .Select(CreateMovePlan)
                .GroupBy(y => y.EstimatedValue * (int)nextToMove)
                .OrderByDescending(x => x.Key)
                .First().ToArray();
        }

        private static MovePlan CreateMovePlan(PreliminaryStateAndMove stateAndMove, MovePlan plan)
        {
            var chainedMoves = new[] { stateAndMove }.Concat(plan.ChainedMoves).ToArray();
            return new MovePlan(chainedMoves, plan.EstimatedValue);
        }

        private MovePlan CreateMovePlan(PreliminaryStateAndMove preliminaryStateAndMove)
        {
            var estimatedValue = EvaluateMove(preliminaryStateAndMove.PreliminaryState.ChessGrid,
                preliminaryStateAndMove.NextMove);
            return new MovePlan(new[] { preliminaryStateAndMove }, estimatedValue);
        }

        private float GetValueOfPieces(ChessGrid grid)
        {
            var value = grid.GetAllChessPiecePositions()
                .Sum(x => Calculations.PieceValues[x.ChessPiece.PieceType] * (int)x.ChessPiece.Player);

            // Double queen value
            var queens = grid.GetAllChessPiecePositions().Where(x => x.ChessPiece.PieceType == PieceType.Queen).ToList();
            value += queens.Sum(x => (int)x.ChessPiece.Player * 9);
            return value;
        }




        private float EvaluateMove(ChessGrid chessGrid, ChessMove move)
        {
            var evaluatedValue = 0f;

            var gridAfterMove = _chessCalculationsService.GetGridAfterMove(chessGrid, move);

            evaluatedValue += GetValueOfPieces(gridAfterMove);

            var positions = gridAfterMove.GetAllChessPiecePositions().ToArray();

            evaluatedValue += (float)EvaluatePiecesForPlayer(Player.White, positions, PieceType.Bishop);
            evaluatedValue += (float)EvaluatePiecesForPlayer(Player.Black, positions, PieceType.Bishop);

            //evaluatedValue += (float)EvaluatePiecesForPlayer(Player.White, positions, PieceType.Knight);
            //evaluatedValue += (float)EvaluatePiecesForPlayer(Player.Black, positions, PieceType.Knight);

            evaluatedValue += (float)EvaluatePiecesForPlayer(Player.White, positions, PieceType.Rook);
            evaluatedValue += (float)EvaluatePiecesForPlayer(Player.Black, positions, PieceType.Rook);

            foreach (var position in positions)
            {
                switch (position.ChessPiece.PieceType)
                {
                    case PieceType.Rook:
                        evaluatedValue += EvaluateRook(position, gridAfterMove);
                        break;
                    case PieceType.Queen:
                        evaluatedValue += EvaluateRook(position, gridAfterMove);
                        break;
                    case PieceType.Pawn:
                        evaluatedValue += EvaluatePawn(position, gridAfterMove);
                        break;
                    default:
                        evaluatedValue += EvaluatePosition(position);
                        break;

                }
            }

            return evaluatedValue;
        }



        private float EvaluatePosition(ChessPiecePosition position)
        {
            float value = 0f;

            var player = (int) position.ChessPiece.Player;
            
            var colMaxLimit = 3;
            var colMinLimit = 6;
            
            var row = (int) position.ChessPosition.Row;
            var column = (int) position.ChessPosition.Column;

            if (column >= colMinLimit && column <= colMaxLimit)
            {
                value += 0.5f;
            }

            if (position.ChessPiece.Player == Player.Black && row < 6)
            {
                value += 0.5f;
            }
            else if(position.ChessPiece.Player == Player.White && column > 3)
            {
                value += 0.5f;
            }
            return value * player;
        }

        private float EvaluatePawn(ChessPiecePosition position, ChessGrid gridAfterMove)
        {
            float value = 0f;

            var positions = gridAfterMove.GetAllChessPiecePositions().ToArray();
            var isBlocking = positions.Any(x => (int)x.ChessPosition.Row == (int) position.ChessPosition.Row + (int) position.ChessPiece.Player
                                    && x.ChessPosition.Column == position.ChessPosition.Column);

            //subtract points for blocked pawns
            if (isBlocking)
            {
                value -= 0.5f * (int)position.ChessPiece.Player;
            }
            //give points for outwardly pawn
            value += (int) position.ChessPosition.Row * (int)position.ChessPiece.Player;
            
            return value;


        }

        private float EvaluateRook(ChessPiecePosition position, ChessGrid grid)
        {
            float value = 0;

            var row = (int)position.ChessPosition.Row;
            var column = (int)position.ChessPosition.Column;
            var tempRow = row;
            var tempColumn = column;

            var positions = grid.GetAllChessPiecePositions().ToArray();

            var reachableSquares = 0;

            tempColumn = column;
            tempColumn++;
            while (tempColumn < 8)
            {
                if (positions
                    .Where(x => (int)x.ChessPosition.Row == tempRow && (int)x.ChessPosition.Column == tempColumn)
                    .ToList()
                    .Count == 0)
                {
                    reachableSquares += 1;
                }
                else
                {
                    tempColumn = 8;
                }
                tempColumn++;
            }
            tempColumn = column;
            tempColumn--;
            while (tempColumn >= 0)
            {
                if (positions
                    .Where(x => (int)x.ChessPosition.Row == tempRow && (int)x.ChessPosition.Column == tempColumn)
                    .ToList()
                    .Count == 0)
                {
                    reachableSquares += 1;
                }
                else
                {
                    tempColumn = -1;
                }
                tempColumn--;
            }
            tempColumn = column;
            tempRow = row;
            tempRow++;
            while (tempRow < 8)
            {
                if (positions
                    .Where(x => (int)x.ChessPosition.Row == tempRow && (int)x.ChessPosition.Column == tempColumn)
                    .ToList()
                    .Count == 0)
                {
                    reachableSquares += 1;
                }
                else
                {
                    tempRow = 8;
                }
                tempRow++;
            }
            tempRow = row;
            tempRow--;
            while (tempRow >= 0)
            {
                if (positions
                    .Where(x => (int)x.ChessPosition.Row == tempRow && (int)x.ChessPosition.Column == tempColumn)
                    .ToList()
                    .Count == 0)
                {
                    reachableSquares += 1;
                }
                else
                {
                    tempRow = -1;
                }
                tempRow--;
            }

            value += reachableSquares * 0.1f;

            return value * (int)position.ChessPiece.Player;
        }

        private double EvaluatePiecesForPlayer(Player player, ChessPiecePosition[] positions, PieceType pieceType)
        {
            var pieces = positions.Where(x => x.ChessPiece.Player == player);

            var count = pieces.Count(p => p.ChessPiece.PieceType == pieceType);

            var playerValue = player == Player.White ? 1 : -1;
            if (count > 1)
            {
                switch (pieceType)
                {
                    case PieceType.Bishop:
                        return playerValue * 1;
                    case PieceType.Knight:
                        return playerValue * 0.2;
                    case PieceType.Rook:
                        return playerValue * 0.2;
                }
            }
            return 0;
        }
    }
}
