using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Geek2k16.Entities;
using Geek2k16.Entities.Constants;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using A = Geek2k16.Entities.Enums.Abbr;
using POS = Geek2k16.Entities.Enums.PieceType;
using COL = Geek2k16.Entities.Enums.ChessColumn;
using ROW = Geek2k16.Entities.Enums.ChessRow;

namespace Geek2K16.Service.Tests
{
    [TestClass]
    public class ChessCalculationsServiceTests
    {
        private readonly ChessCalculationsService _chessCalculationsService;

        public ChessCalculationsServiceTests()
        {
            _chessCalculationsService = new ChessCalculationsService();
        }

        #region GetValueOfPieces

        [TestMethod]
        public void OnePlyChallenge_CanTakeOutPawnAsBestChoice_TakeOutPawnWithBishop()
        {
            A?[,] startGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, null, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WP, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WB, A.WP},
                {A.WR, null, null, null, null, A.WR, A.WK, A.WQ}
            };
            var state = _chessCalculationsService.GetStateFromGrid(startGrid);
            var availableMoves = _chessCalculationsService.GetAvailableMoves(state);
            var movesWithNextPly = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(state,
                availableMoves);
            var legalMoves = _chessCalculationsService.FilterBySelfMatingMoves(movesWithNextPly, state);
            var moveDictionary = legalMoves.ToDictionary(x => x,
                x => _chessCalculationsService.GetValueOfPieces(
                    _chessCalculationsService.GetGridAfterMove(state.ChessGrid, x)));
            var bestMove = moveDictionary.OrderByDescending(x => x.Value).First();
            Assert.AreEqual(new ChessMove(Player.White, POS.Bishop, COL.G, ROW.Row2, COL.D, ROW.Row5), bestMove.Key);
        }

        [TestMethod]
        public void TwoPlyChallenge_IfTakeOutPawnWillLooseBishop_ShouldNotTakeOutPawn()
        {
            A?[,] startGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, null, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WP, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WB, A.WP},
                {A.WR, null, null, null, null, A.WR, A.WK, A.WQ}
            };
            var state = _chessCalculationsService.GetStateFromGrid(startGrid);

            var availableMoves = _chessCalculationsService.GetAvailableMoves(state);
            var movesWithNextPly = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(state,
                availableMoves);
            var legalMoves = _chessCalculationsService.FilterBySelfMatingMoves(movesWithNextPly, state);
            var legalMovesWithNextPly = movesWithNextPly.Where(x => legalMoves.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            var legalMovesWithNextValues = legalMovesWithNextPly.ToDictionary(x => x.Key, x =>
            {
                var firstMove = x.Key;
                var stateAfterFirstMove = _chessCalculationsService.GetGridAfterMove(state.ChessGrid, firstMove);
                return x.Value.Select(
                        m =>
                                _chessCalculationsService.GetGridAfterMove(stateAfterFirstMove, m))
                    .Select(s => _chessCalculationsService.GetValueOfPieces(s))
                    .OrderBy(y => y*(int) state.NextToMove).First();
            });
            var bestMove = legalMovesWithNextValues.OrderByDescending(x => x.Value).First();

            Assert.AreNotEqual(new ChessMove(Player.White, POS.Bishop, COL.G, ROW.Row2, COL.D, ROW.Row5), bestMove.Key);
        }

        [TestMethod]
        public void ThreePlyChallenge_IfTakeOutPawnWithBishopThenProtectWithQueen_ShouldTakeOutPawn()
        {
            A?[,] startGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, null, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WP, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WB, A.WP},
                {A.WR, null, null, null, null, A.WR, A.WK, A.WQ}
            };
            var rootState = _chessCalculationsService.GetPreliminaryStateFromGrid(startGrid);
            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = _chessCalculationsService
                .GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService
                .FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

            var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                .ToDictionary(x => x.Key, x => x.Value);

            var availableStateMovesGen2ToGen3 = legalStateMovesGen1ToGen2.Values
                .Select(g2 => _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(g2))
                .ToArray();

            var gen2ToGen3Evaluations = availableStateMovesGen2ToGen3
                .Select(x => x.ToDictionary(g2 => g2.Key, g2 => _chessCalculationsService.GetBestPlans(g2.Value).First()))
                .SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);

            var gen1ToGen2Evaluations = legalStateMovesGen1ToGen2
                .ToDictionary(g1 => g1.Key, g1 => _chessCalculationsService.GetBestPlans(g1.Value, gen2ToGen3Evaluations).First());

            var bestPlan = _chessCalculationsService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations).First();
            var bestMove = bestPlan.ChainedMoves.First();

            Assert.AreEqual(new ChessMove(Player.White, POS.Bishop, COL.G, ROW.Row2, COL.D, ROW.Row5), bestMove.NextMove);
        }

        [TestMethod]
        public void ThreePlyChallenge_IfTakeOutPawnWithBishopThenUnprotected_ShouldNotTakeOutPawn()
        {
            A?[,] startGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, null, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WP, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WB, A.WP},
                {A.WR, null, null, null, null, A.WR, A.WK, null}
            };
            var rootState = _chessCalculationsService.GetPreliminaryStateFromGrid(startGrid);
            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = _chessCalculationsService
                .GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService
                .FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

            var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                .ToDictionary(x => x.Key, x => x.Value);

            var availableStateMovesGen2ToGen3 = legalStateMovesGen1ToGen2.Values
                .Select(g2 => _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(g2))
                .ToArray();

            var gen2ToGen3Evaluations = availableStateMovesGen2ToGen3
                .Select(x => x.ToDictionary(g2 => g2.Key, g2 => _chessCalculationsService.GetBestPlans(g2.Value).First()))
                .SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);

            var gen1ToGen2Evaluations = legalStateMovesGen1ToGen2
                .ToDictionary(g1 => g1.Key, g1 => _chessCalculationsService.GetBestPlans(g1.Value, gen2ToGen3Evaluations).First());

            var bestPlan = _chessCalculationsService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations).First();
            var bestMove = bestPlan.ChainedMoves.First();

            Assert.AreNotEqual(new ChessMove(Player.White, POS.Bishop, COL.G, ROW.Row2, COL.D, ROW.Row5), bestMove.NextMove);
        }

        #endregion

        #region GetIntermediatePositions

        [TestMethod]
        public void GetIntermediatePositions_BishopMovingThreeSquares_ReturnTwoInbetween()
        {
            var endPosition = new ChessPosition(COL.C, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Bishop, Player.Black);
            var startPosition = new ChessPosition(COL.F, ROW.Row8);
            var bishopF8ToC5 = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);

            var expectedPositions = new[] {new ChessPosition(COL.D, ROW.Row6), new ChessPosition(COL.E, ROW.Row7)};
            var intermediatePositions = _chessCalculationsService.GetIntermediatePositions(bishopF8ToC5).ToArray();

            CollectionAssert.AreEquivalent(intermediatePositions, expectedPositions);
        }

        #endregion

        #region GetStateAfterMove: StateFlags

        [TestMethod]
        public void GetStateAfterMove_StateFlag_MovedWhiteHRook_FlaggedAsMoved()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.H, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(chessState, move);
            Assert.IsTrue(newState.StateFlags?.HasFlag(StateFlag.WhiteHRookHasMoved) == true);
        }

        [TestMethod]
        public void GetStateAfterMove_StateFlag_MovedBlackHRook_FlaggedAsMoved()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.F, ROW.Row8);
            var chessPiece = new ChessPiece(POS.Rook, Player.Black);
            var startPosition = new ChessPosition(COL.H, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(chessState, move);
            Assert.IsTrue(newState.StateFlags?.HasFlag(StateFlag.BlackHRookHasMoved) == true);
        }

        [TestMethod]
        public void GetStateAfterMove_StateFlag_MovedBlackKing_FlaggedAsMoved()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.F, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(chessState, move);
            Assert.IsTrue(newState.StateFlags?.HasFlag(StateFlag.BlackKingHasMoved) == true);
        }

        [TestMethod]
        public void GetStateAfterMove_StateFlag_MovedBlackARook_FlaggedAsMoved()
        {
            A?[,] grid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.B, ROW.Row8);
            var chessPiece = new ChessPiece(POS.Rook, Player.Black);
            var startPosition = new ChessPosition(COL.A, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(chessState, move);
            Assert.IsTrue(newState.StateFlags?.HasFlag(StateFlag.BlackARookHasMoved) == true);
        }

        [TestMethod]
        public void GetStateAfterMove_StateFlag_MovedWhiteARook_FlaggedAsMoved()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.A, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(chessState, move);
            Assert.IsTrue(newState.StateFlags?.HasFlag(StateFlag.WhiteARookHasMoved) == true);
        }

        [TestMethod]
        public void GetStateAfterMove_StateFlag_MovedWhiteKing_FlaggedAsMoved()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.D, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(chessState, move);
            Assert.IsTrue(newState.StateFlags?.HasFlag(StateFlag.WhiteKingHasMoved) == true);
        }

        #endregion

        #region GetStateAfterMove: EndResult

        [TestMethod]
        public void EndResult_MovedKnightsBackAndForthThreeTimes_RepeatStateAsEndResult()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition2 = new ChessPosition(COL.C, ROW.Row3);
            var chessPiece2 = new ChessPiece(POS.Knight, Player.White);
            var startPosition2 = new ChessPosition(COL.B, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece2, startPosition2), endPosition2);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var endPosition = new ChessPosition(COL.C, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Knight, Player.Black);
            var startPosition = new ChessPosition(COL.B, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var blackMove1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var endPosition3 = new ChessPosition(COL.B, ROW.Row1);
            var chessPiece3 = new ChessPiece(POS.Knight, Player.White);
            var startPosition3 = new ChessPosition(COL.C, ROW.Row3);
            var whiteChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece3, startPosition3), endPosition3);
            var whiteMove2 = new ExecutedChessMove(whiteChessMove1, new TimeSpan());
            var endPosition1 = new ChessPosition(COL.B, ROW.Row8);
            var chessPiece1 = new ChessPiece(POS.Knight, Player.Black);
            var startPosition1 = new ChessPosition(COL.C, ROW.Row6);
            var blackChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var blackMove2 = new ExecutedChessMove(blackChessMove1, new TimeSpan());

            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);
            var state2 = _chessCalculationsService.GetStateAfterMove(state1, blackMove1);
            var state3 = _chessCalculationsService.GetStateAfterMove(state2, whiteMove2);
            var state4 = _chessCalculationsService.GetStateAfterMove(state3, blackMove2);
            var state5 = _chessCalculationsService.GetStateAfterMove(state4, whiteMove1);
            var state6 = _chessCalculationsService.GetStateAfterMove(state5, blackMove1);
            var state7 = _chessCalculationsService.GetStateAfterMove(state6, whiteMove2);
            var state8 = _chessCalculationsService.GetStateAfterMove(state7, blackMove2);

            Assert.IsTrue(state8.EndResult == StateResult.RepeatStateThreeTimes);
        }

        [TestMethod]
        public void EndResult_WhiteUseTooMuchTime_OutOfTimeEndResult()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition = new ChessPosition(COL.C, ROW.Row3);
            var chessPiece = new ChessPiece(POS.Knight, Player.White);
            var startPosition = new ChessPosition(COL.B, ROW.Row1);
            var whiteMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var executedMove = new ExecutedChessMove(whiteMove, RuleConstants.StartTime.Add(TimeSpan.FromMinutes(1)));
            var newState = _chessCalculationsService.GetStateAfterMove(state, executedMove);

            Assert.AreEqual(StateResult.WhiteIsOutOfTime, newState.EndResult);
        }

        [TestMethod]
        public void EndResult_WhiteKingMovesOntoWhitePawn_WhiteIllegalMove()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, null, null, A.WP},
                {null, null, null, null, null, A.BK, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            var state1 = chessBoardState;
            var whiteChessMove = new ChessMove(Player.White, POS.King, COL.H, ROW.Row8, COL.H, ROW.Row7);
            var whiteMove = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(state1, whiteMove);

            Assert.AreEqual(StateResult.WhiteIllegalMove, newState.EndResult);
        }

        [TestMethod]
        public void EndResult_NoLegalMoves_Stalemate()
        {
            A?[,] staleMateGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, null, null, A.WP},
                {null, null, null, null, null, A.BK, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(staleMateGrid, Player.Black);
            var state1 = chessBoardState;
            var endPosition = new ChessPosition(COL.F, ROW.Row7);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.F, ROW.Row6);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var blackMove1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(state1, blackMove1);

            Assert.AreEqual(StateResult.Stalemate, newState.EndResult);
        }

        #endregion

        #region GetStateAfterMove: ResultingChessPositions: Promotion

        [TestMethod]
        public void GetStateAfterMove_Promotion_BlackPawnMovesToLastRowAndBecomesQueen()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, A.BP, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row2);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToQueen);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var condition = _chessCalculationsService.GetStateAfterMove(chessState, move)
                .ChessGrid.GetAllChessPiecePositions().Contains(new ChessPiecePosition(new ChessPiece(POS.Queen, Player.Black), new ChessPosition(COL.C, ROW.Row1)));
            Assert.IsTrue(condition);
        }

        [TestMethod]
        public void GetStateAfterMove_Promotion_BlackPawnMovesToLastRowAndBecomesKnight()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, A.BP, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row2);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToKnight);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var condition = _chessCalculationsService.GetStateAfterMove(chessState, move)
                .ChessGrid.GetAllChessPiecePositions().Contains(new ChessPiecePosition(new ChessPiece(POS.Knight, Player.Black), new ChessPosition(COL.C, ROW.Row1)));
            Assert.IsTrue(condition);
        }

        [TestMethod]
        public void GetStateAfterMove_Promotion_WhitePawnMovesToLastRowAndBecomesQueen()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.H, ROW.Row8);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.H, ROW.Row7);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToQueen);
            var move = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var condition = _chessCalculationsService.GetStateAfterMove(chessState, move)
                .ChessGrid.GetAllChessPiecePositions().Contains(new ChessPiecePosition(new ChessPiece(POS.Queen, Player.White), new ChessPosition(COL.H, ROW.Row8)));
            Assert.IsTrue(condition);
        }

        [TestMethod]
        public void GetStateAfterMove_Promotion_WhitePawnMovesToLastRowAndBecomesKnight()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.H, ROW.Row8);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.H, ROW.Row7);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToKnight);
            var move = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var condition = _chessCalculationsService.GetStateAfterMove(chessState, move)
                .ChessGrid.GetAllChessPiecePositions().Contains(new ChessPiecePosition(new ChessPiece(POS.Knight, Player.White), new ChessPosition(COL.H, ROW.Row8)));
            Assert.IsTrue(condition);
        }

        #endregion

        #region GetStateAfterMove: ResultingChessPositions: Castling

        [TestMethod]
        public void GetStateAfterMove_Castling_BlackHasCastledLong_RookA8ToD8()
        {
            A?[,] grid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.C, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var allChessPiecePositions =
                _chessCalculationsService.GetStateAfterMove(chessState, move).ChessGrid
                    .GetAllChessPiecePositions().ToArray();
            var condition = allChessPiecePositions.Contains(new ChessPiecePosition(new ChessPiece(POS.Rook, Player.Black), new ChessPosition(COL.D, ROW.Row8)));
            Assert.IsTrue(condition);
        }

        [TestMethod]
        public void GetStateAfterMove_Castling_BlackHasCastledLong_NoRookInA8()
        {
            A?[,] grid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.C, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var allChessPiecePositions =
                _chessCalculationsService.GetStateAfterMove(chessState, move).ChessGrid
                    .GetAllChessPiecePositions().ToArray();
            var conditionOldRook = allChessPiecePositions.Contains(new ChessPiecePosition(new ChessPiece(POS.Rook, Player.Black), new ChessPosition(COL.A, ROW.Row8)));
            Assert.IsFalse(conditionOldRook);
        }

        [TestMethod]
        public void GetStateAfterMove_Castling_BlackHasCastledShort_RookH8ToF8()
        {
            A?[,] grid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var condition = _chessCalculationsService.GetStateAfterMove(chessState, move).ChessGrid
                .GetAllChessPiecePositions().Contains(new ChessPiecePosition(new ChessPiece(POS.Rook, Player.Black), new ChessPosition(COL.F, ROW.Row8)));
            Assert.IsTrue(condition);
        }

        [TestMethod]
        public void GetStateAfterMove_BlackHasCastledShortAndThenNewMove_FlagBlackKingAsMoved()
        {
            A?[,] grid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            
            
            var chessState = chessBoardState;
            var endPosition1 = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece1 = new ChessPiece(POS.King, Player.Black);
            var startPosition1 = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var move = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var stateAfterBlackMove = _chessCalculationsService.GetStateAfterMove(chessState, move);
            var endPosition = new ChessPosition(COL.D, ROW.Row4);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row5);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var whiteMove = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var newState = _chessCalculationsService.GetStateAfterMove(stateAfterBlackMove, whiteMove);

            Assert.IsTrue(newState.StateFlags?.HasFlag(StateFlag.BlackKingHasMoved) == true);
        }

        [TestMethod]
        public void GetStateAfterMove_Castling_WhiteHasCastledLong_RookA1ToD1()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var condition = _chessCalculationsService.GetStateAfterMove(chessState, move).ChessGrid
                .GetAllChessPiecePositions().Contains(new ChessPiecePosition(new ChessPiece(POS.Rook, Player.White), new ChessPosition(COL.D, ROW.Row1)));
            Assert.IsTrue(condition);
        }

        [TestMethod]
        public void GetStateAfterMove_Castling_WhiteHasCastledShort_RookH1ToF1()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            
            
            var chessState = chessBoardState;
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var move = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var condition = _chessCalculationsService.GetStateAfterMove(chessState, move).ChessGrid
                .GetAllChessPiecePositions().Contains(new ChessPiecePosition(new ChessPiece(POS.Rook, Player.White), new ChessPosition(COL.F, ROW.Row1)));
            Assert.IsTrue(condition);
        }

        #endregion

        #region GetMoveHistoryText + HashHistory

        [TestMethod]
        public void MoveHistory_MovedWhiteKnight_1_Nc3()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition = new ChessPosition(COL.C, ROW.Row3);
            var chessPiece = new ChessPiece(POS.Knight, Player.White);
            var startPosition = new ChessPosition(COL.B, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());

            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);
            var moveHistory = _chessCalculationsService.GetMoveHistoryText(state1);

            Assert.AreEqual("1. Nc3", moveHistory);
        }

        [TestMethod]
        public void MoveHistory_ThreeMoves_1_d4_e5_2_d5()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition1 = new ChessPosition(COL.D, ROW.Row4);
            var chessPiece1 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition1 = new ChessPosition(COL.D, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var endPosition = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var blackMove1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var endPosition2 = new ChessPosition(COL.D, ROW.Row5);
            var chessPiece2 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition2 = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece2, startPosition2), endPosition2);
            var whiteMove2 = new ExecutedChessMove(whiteChessMove1, new TimeSpan());

            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);
            var state2 = _chessCalculationsService.GetStateAfterMove(state1, blackMove1);
            var state3 = _chessCalculationsService.GetStateAfterMove(state2, whiteMove2);
            var moveHistory = _chessCalculationsService.GetMoveHistoryText(state3);

            Assert.AreEqual("1. d4  e5  2. d5", moveHistory);
        }

        [TestMethod]
        public void MoveHistory_FastCheck_1_e4_e5_2_Qh5_Nc6_3_Qxf7plus()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition2 = new ChessPosition(COL.E, ROW.Row4);
            var chessPiece2 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition2 = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece2, startPosition2), endPosition2);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var endPosition = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var blackMove1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var endPosition3 = new ChessPosition(COL.H, ROW.Row5);
            var chessPiece3 = new ChessPiece(POS.Queen, Player.White);
            var startPosition3 = new ChessPosition(COL.D, ROW.Row1);
            var whiteChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece3, startPosition3), endPosition3);
            var whiteMove2 = new ExecutedChessMove(whiteChessMove1, new TimeSpan());
            var endPosition1 = new ChessPosition(COL.C, ROW.Row6);
            var chessPiece1 = new ChessPiece(POS.Knight, Player.Black);
            var startPosition1 = new ChessPosition(COL.B, ROW.Row8);
            var blackChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var blackMove2 = new ExecutedChessMove(blackChessMove1, new TimeSpan());
            var endPosition4 = new ChessPosition(COL.F, ROW.Row7);
            var chessPiece4 = new ChessPiece(POS.Queen, Player.White);
            var startPosition4 = new ChessPosition(COL.H, ROW.Row5);
            var whiteChessMove2 = new ChessMove(new ChessPiecePosition(chessPiece4, startPosition4), endPosition4);
            var whiteMove3 = new ExecutedChessMove(whiteChessMove2, new TimeSpan());

            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);
            var state2 = _chessCalculationsService.GetStateAfterMove(state1, blackMove1);
            var state3 = _chessCalculationsService.GetStateAfterMove(state2, whiteMove2);
            var state4 = _chessCalculationsService.GetStateAfterMove(state3, blackMove2);
            var state5 = _chessCalculationsService.GetStateAfterMove(state4, whiteMove3);
            var moveHistory = _chessCalculationsService.GetMoveHistoryText(state5);

            Assert.AreEqual("1. e4  e5  2. Qh5  Nc6  3. Qxf7+", moveHistory);
        }

        [TestMethod]
        public void MoveHistory_SchoolCheckMate_1_e4_e5_2_Qh5_Nc6_3_Bc4_Nf6_4_Qxf7hash()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition3 = new ChessPosition(COL.E, ROW.Row4);
            var chessPiece3 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition3 = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece3, startPosition3), endPosition3);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var endPosition = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var blackMove1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var endPosition4 = new ChessPosition(COL.H, ROW.Row5);
            var chessPiece4 = new ChessPiece(POS.Queen, Player.White);
            var startPosition4 = new ChessPosition(COL.D, ROW.Row1);
            var whiteChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece4, startPosition4), endPosition4);
            var whiteMove2 = new ExecutedChessMove(whiteChessMove1, new TimeSpan());
            var endPosition1 = new ChessPosition(COL.C, ROW.Row6);
            var chessPiece1 = new ChessPiece(POS.Knight, Player.Black);
            var startPosition1 = new ChessPosition(COL.B, ROW.Row8);
            var blackChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var blackMove2 = new ExecutedChessMove(blackChessMove1, new TimeSpan());
            var endPosition5 = new ChessPosition(COL.C, ROW.Row4);
            var chessPiece5 = new ChessPiece(POS.Bishop, Player.White);
            var startPosition5 = new ChessPosition(COL.F, ROW.Row1);
            var whiteChessMove2 = new ChessMove(new ChessPiecePosition(chessPiece5, startPosition5), endPosition5);
            var whiteMove3 = new ExecutedChessMove(whiteChessMove2, new TimeSpan());
            var endPosition2 = new ChessPosition(COL.F, ROW.Row6);
            var chessPiece2 = new ChessPiece(POS.Knight, Player.Black);
            var startPosition2 = new ChessPosition(COL.G, ROW.Row8);
            var blackChessMove2 = new ChessMove(new ChessPiecePosition(chessPiece2, startPosition2), endPosition2);
            var blackMove3 = new ExecutedChessMove(blackChessMove2, new TimeSpan());
            var endPosition6 = new ChessPosition(COL.F, ROW.Row7);
            var chessPiece6 = new ChessPiece(POS.Queen, Player.White);
            var startPosition6 = new ChessPosition(COL.H, ROW.Row5);
            var whiteChessMove3 = new ChessMove(new ChessPiecePosition(chessPiece6, startPosition6), endPosition6);
            var whiteMove4 = new ExecutedChessMove(whiteChessMove3, new TimeSpan());

            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);
            var state2 = _chessCalculationsService.GetStateAfterMove(state1, blackMove1);
            var state3 = _chessCalculationsService.GetStateAfterMove(state2, whiteMove2);
            var state4 = _chessCalculationsService.GetStateAfterMove(state3, blackMove2);
            var state5 = _chessCalculationsService.GetStateAfterMove(state4, whiteMove3);
            var state6 = _chessCalculationsService.GetStateAfterMove(state5, blackMove3);
            var state7 = _chessCalculationsService.GetStateAfterMove(state6, whiteMove4);
            var moveHistory = _chessCalculationsService.GetMoveHistoryText(state7);

            Assert.AreEqual("1. e4  e5  2. Qh5  Nc6  3. Bc4  Nf6  4. Qxf7#  0-1", moveHistory);
        }

        [TestMethod]
        public void MoveHistory_LongCastle_0_0_0()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var state = _chessCalculationsService.GetStateFromGrid(castleLongGrid);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);

            Assert.AreEqual("0-0-0", state1.LastMove?.Caption);
        }

        [TestMethod, Ignore]
        public void MoveHistory_MoveARook_Rac1()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WK, A.WP, A.WP},
                {A.WR, null, null, null, null, null, null, A.WR}
            };

            var state = _chessCalculationsService.GetStateFromGrid(grid);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.A, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);

            Assert.AreEqual("Rac1", state1.LastMove?.Caption);
        }

        [TestMethod]
        public void MoveHistory_RookTakes_Rxe6()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, null},
                {null, null, null, null, A.BR, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WR, null, null, null},
                {null, null, null, null, A.WK, null, null, null}
            };

            var state = _chessCalculationsService.GetStateFromGrid(grid);
            var endPosition = new ChessPosition(COL.E, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);

            Assert.AreEqual("Rxe6", state1.LastMove?.Caption);
        }

        [TestMethod, Ignore]
        public void MoveHistory_EColumnRookTakes_Rexe6()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, null},
                {null, A.WR, null, null, A.BR, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WR, null, null, null},
                {null, null, null, null, A.WK, null, null, null}
            };

            var state = _chessCalculationsService.GetStateFromGrid(grid);
            var endPosition = new ChessPosition(COL.E, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);

            Assert.AreEqual("Rexe6", state1.LastMove?.Caption);
        }

        [TestMethod, Ignore]
        public void MoveHistory_Row2RookTakes_R2xe6()
        {
            A?[,] grid =
            {
                {null, null, null, null, A.WR, null, null, null},
                {null, null, null, null, null, A.BK, null, null},
                {null, null, null, null, A.BR, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WR, null, null, null},
                {null, null, null, null, A.WK, null, null, null}
            };

            var state = _chessCalculationsService.GetStateFromGrid(grid);
            var endPosition = new ChessPosition(COL.E, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);

            Assert.AreEqual("R2xe6", state1.LastMove?.Caption);
        }

        [TestMethod]
        public void MoveHistory_BlackPromotionToQueen_c1_Q()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, A.BP, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var state = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row2);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToQueen);
            var pawnC2ToC1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var state1 = _chessCalculationsService.GetStateAfterMove(state, pawnC2ToC1);

            Assert.AreEqual("c1=Q", state1.LastMove?.Caption);
        }

        [TestMethod]
        public void MoveHistory_BlackPromotionToKnight_c1_N()
        {
            A?[,] grid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, A.BP, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var state = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row2);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToKnight);
            var pawnC2ToC1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var state1 = _chessCalculationsService.GetStateAfterMove(state, pawnC2ToC1);

            Assert.AreEqual("c1=N", state1.LastMove?.Caption);
        }

        [TestMethod]
        public void MoveHistory_ShortCastle_0_0()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var state = _chessCalculationsService.GetStateFromGrid(castleShortGrid);
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);

            Assert.AreEqual("0-0", state1.LastMove?.Caption);
        }

        [TestMethod]
        public void MoveHistory_WhitePawnTookBlackPawn_dxe5()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition1 = new ChessPosition(COL.D, ROW.Row4);
            var chessPiece1 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition1 = new ChessPosition(COL.D, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var endPosition = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var blackMove1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var endPosition2 = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece2 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition2 = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece2, startPosition2), endPosition2);
            var whiteMove2 = new ExecutedChessMove(whiteChessMove1, new TimeSpan());

            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);
            var state2 = _chessCalculationsService.GetStateAfterMove(state1, blackMove1);
            var state3 = _chessCalculationsService.GetStateAfterMove(state2, whiteMove2);

            Assert.AreEqual("dxe5", state3.LastMove?.Caption);
        }

        [TestMethod]
        public void HashHistory_MovedPawnsThreeTimes_UniqueHashValues()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition1 = new ChessPosition(COL.D, ROW.Row4);
            var chessPiece1 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition1 = new ChessPosition(COL.D, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var endPosition = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var blackMove1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var endPosition2 = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece2 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition2 = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece2, startPosition2), endPosition2);
            var whiteMove2 = new ExecutedChessMove(whiteChessMove1, new TimeSpan());

            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);
            var state2 = _chessCalculationsService.GetStateAfterMove(state1, blackMove1);
            var state3 = _chessCalculationsService.GetStateAfterMove(state2, whiteMove2);

            Assert.AreEqual(4, state3.GridHashHistory.Count);
            CollectionAssert.AllItemsAreUnique(state3.GridHashHistory.ToList());
        }

        [TestMethod]
        public void HashHistory_MovedKnightsBackAndForth_TwoIdenticalHashes()
        {
            var state = _chessCalculationsService.GetInitialState();
            var endPosition2 = new ChessPosition(COL.C, ROW.Row3);
            var chessPiece2 = new ChessPiece(POS.Knight, Player.White);
            var startPosition2 = new ChessPosition(COL.B, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece2, startPosition2), endPosition2);
            var whiteMove1 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var endPosition = new ChessPosition(COL.C, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Knight, Player.Black);
            var startPosition = new ChessPosition(COL.B, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var blackMove1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var endPosition3 = new ChessPosition(COL.B, ROW.Row1);
            var chessPiece3 = new ChessPiece(POS.Knight, Player.White);
            var startPosition3 = new ChessPosition(COL.C, ROW.Row3);
            var whiteChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece3, startPosition3), endPosition3);
            var whiteMove2 = new ExecutedChessMove(whiteChessMove1, new TimeSpan());
            var endPosition1 = new ChessPosition(COL.B, ROW.Row8);
            var chessPiece1 = new ChessPiece(POS.Knight, Player.Black);
            var startPosition1 = new ChessPosition(COL.C, ROW.Row6);
            var blackChessMove1 = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var blackMove2 = new ExecutedChessMove(blackChessMove1, new TimeSpan());

            var state1 = _chessCalculationsService.GetStateAfterMove(state, whiteMove1);
            var state2 = _chessCalculationsService.GetStateAfterMove(state1, blackMove1);
            var state3 = _chessCalculationsService.GetStateAfterMove(state2, whiteMove2);
            var state4 = _chessCalculationsService.GetStateAfterMove(state3, blackMove2);

            Assert.AreEqual(5, state4.GridHashHistory.Count);
            Assert.IsTrue(state4.GridHashHistory.GroupBy(x => x).Any(x => x.Count() == 2));
        }

        #endregion

        #region ValueCalculations

        [TestMethod]
        public void GetValueOfPieces_InitialGrid_Return0()
        {
            var state = _chessCalculationsService.GetInitialState();
            Assert.AreEqual(0.0f, _chessCalculationsService.GetValueOfPieces(state));
        }

        [TestMethod]
        public void GetValueOfPieces_RemoveBlackQueen_ReturnPositive9()
        {
            A?[,] initialGrid =
            {
                {A.BR, A.BN, A.BB, null, A.BK, A.BB, A.BN, A.BR},
                {A.BP, A.BP, A.BP, A.BP, A.BP, A.BP, A.BP, A.BP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, A.WP, A.WP, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var state = _chessCalculationsService.GetStateFromGrid(initialGrid);
            Assert.AreEqual(9.0f, _chessCalculationsService.GetValueOfPieces(state));
        }

        #endregion

        #region Attacks

        [TestMethod]
        public void GetLegalMoves_PawnAttack_WhitePawnIsDirectlyOppositeOfBlackPawn_WhitePawnCannotAttack()
        {
            A?[,] enPassantGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, A.WP, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(enPassantGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.D, ROW.Row7);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row6);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD6TakesD7 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, pawnD6TakesD7);
        }

        [TestMethod]
        public void GetLegalMoves_PawnAttack_WhitePawnIsDiagonallyOppositeBlackPawn_WhitePawnCanAttack()
        {
            A?[,] peasantAttackGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, A.WP, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(peasantAttackGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.D, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnE4TakesD5 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, pawnE4TakesD5);
        }

        [TestMethod]
        public void GetLegalMoves_PawnAttack_BlackPawnIsDiagonallyOppositeWhitePawn_BlackPawnCanAttack()
        {
            A?[,] peasantAttackGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, A.WP, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(peasantAttackGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.E, ROW.Row4);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.D, ROW.Row5);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD5TakesE4 = new ExecutedChessMove(blackChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, pawnD5TakesE4);
        }

        [TestMethod]
        public void GetLegalMoves_PawnAttack_WhitePawnIsTwoSquaresAwayBlackPawn_WhitePawnCannotAttack()
        {
            A?[,] peasantAttackGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, A.WP, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, null, A.WP, A.WP, A.WP, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(peasantAttackGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.D, ROW.Row4);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD2TakesD4 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, pawnD2TakesD4);
        }

        [TestMethod]
        public void GetLegalMoves_EnPassant_WhitePawnCanAttackDoubleMoveBlackPawn()
        {
            A?[,] enPassantGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, A.BP, null, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, A.WP, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var endPosition = new ChessPosition(COL.D, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.D, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var lastMove = new ExecutedChessMove(blackChessMove, new TimeSpan());
            var chessBoardState = _chessCalculationsService.GetStateFromGrid(enPassantGrid);
            chessBoardState = AddLastMove(chessBoardState, lastMove.ChessMove);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition1 = new ChessPosition(COL.D, ROW.Row6);
            var chessPiece1 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition1 = new ChessPosition(COL.E, ROW.Row5);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var pawnE5TakesD6 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, pawnE5TakesD6);
        }

        [TestMethod]
        public void GetLegalMoves_EnPassant_BlackPawnCanAttackDoubleMoveWhitePawn()
        {
            A?[,] enPassantGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, null, null, A.BP, A.WP, null, null, null},
                {null, null, null, null, null, null, A.WP, null},
                {null, A.WP, A.WP, A.WP, null, A.WP, null, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var endPosition1 = new ChessPosition(COL.E, ROW.Row4);
            var chessPiece1 = new ChessPiece(POS.Pawn, Player.White);
            var startPosition1 = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece1, startPosition1), endPosition1);
            var lastMove = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            var chessBoardState = _chessCalculationsService.GetStateFromGrid(enPassantGrid, Player.Black);
            chessBoardState = AddLastMove(chessBoardState, lastMove.ChessMove);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.E, ROW.Row3);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD4TakesE3 = new ExecutedChessMove(blackChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, pawnD4TakesE3);
        }

        #endregion

        #region Basic Moves

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CanMoveBishopDown2AndLeft2()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WB, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.B, ROW.Row2);
            var chessPiece = new ChessPiece(POS.Bishop, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopD4ToB2 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, bishopD4ToB2);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_HinderedMove_CannotMoveBishopThroughPawn()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {A.BP, A.BP, A.BP, null, A.BP, null, A.BP, A.BP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, A.BP, null, null},
                {null, null, A.WP, null, null, null, null, null},
                {null, A.WP, null, null, null, null, A.WP, null},
                {A.WP, null, null, A.WP, A.WP, A.WP, null, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Bishop, Player.Black);
            var startPosition = new ChessPosition(COL.F, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopF8ToC5 = new ExecutedChessMove(blackChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, bishopF8ToC5);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CannotMoveBishopRight3()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WB, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row4);
            var chessPiece = new ChessPiece(POS.Bishop, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopD4ToG4 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, bishopD4ToG4);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CannotMoveBishopUp1()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WB, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.D, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Bishop, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopD4ToD5 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, bishopD4ToD5);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CanMoveBishopDown3AndRight3()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WB, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Bishop, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopD4ToG1 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, bishopD4ToG1);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CanMoveBishopUp1AndRight1()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WB, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Bishop, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopD4ToB7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, bishopD4ToB7);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CanMoveBishopUp3AndLeft3()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WB, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.A, ROW.Row7);
            var chessPiece = new ChessPiece(POS.Bishop, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopD4ToA7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, bishopD4ToA7);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CanMoveRookUp3()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WR, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.D, ROW.Row7);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var rookD4ToD7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, rookD4ToD7);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CannotMoveRookUp3AndLeft3()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WR, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.A, ROW.Row7);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var rookD4ToA7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, rookD4ToA7);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CanMoveQueenUp3()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WQ, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.D, ROW.Row7);
            var chessPiece = new ChessPiece(POS.Queen, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var rookD4ToD7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, rookD4ToD7);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_OpenPosition_CannotQueenUp3AndLeft3()
        {
            A?[,] openGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, A.WQ, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(openGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.A, ROW.Row7);
            var chessPiece = new ChessPiece(POS.Queen, Player.White);
            var startPosition = new ChessPosition(COL.D, ROW.Row4);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var queenD4ToA7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, queenD4ToA7);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_InitialGridD3C6D4_CannotMoveC6PawnToC4()
        {
            A?[,] initialGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {A.BP, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {null, null, A.BP, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WP, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(initialGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row4);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row6);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnC6ToC4 = new ExecutedChessMove(blackChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, pawnC6ToC4);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_InitialGridD4_CanMoveC7PawnToC5()
        {
            A?[,] initialGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {A.BP, A.BP, A.BP, A.BP, A.BP, A.BP, A.BP, A.BP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WP, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(initialGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnC7ToC5 = new ExecutedChessMove(blackChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, pawnC7ToC5);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_InitialGridD4_CanMoveC7PawnToC6()
        {
            A?[,] initialGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {A.BP, A.BP, A.BP, A.BP, A.BP, A.BP, A.BP, A.BP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WP, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(initialGrid, Player.Black);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnC7ToC6 = new ExecutedChessMove(blackChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, pawnC7ToC6);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_InitialGrid_CanMoveB2PawnToB3()
        {
            var chessBoardState = _chessCalculationsService.GetStateFromGrid(GridConstants.InitialGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.B, ROW.Row3);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.B, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnB2ToB3 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, pawnB2ToB3);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_InitialGrid_CanMoveB1KnightToC3()
        {
            var chessBoardState = _chessCalculationsService.GetStateFromGrid(GridConstants.InitialGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row3);
            var chessPiece = new ChessPiece(POS.Knight, Player.White);
            var startPosition = new ChessPosition(COL.B, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var knightB1ToC3 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, knightB1ToC3);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_InitialGrid_CannotMoveB2PawnToB5()
        {
            var chessBoardState = _chessCalculationsService.GetStateFromGrid(GridConstants.InitialGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.B, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.B, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnB2ToB5 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, pawnB2ToB5);
        }

        [TestMethod]
        public void GetLegalMoves_BasicMove_InitialGrid_CannotMoveB1BishopToD3()
        {
            var chessBoardState = _chessCalculationsService.GetStateFromGrid(GridConstants.InitialGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.D, ROW.Row3);
            var chessPiece = new ChessPiece(POS.Bishop, Player.White);
            var startPosition = new ChessPosition(COL.B, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopB1ToD3 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, bishopB1ToD3);
        }

        #endregion

        #region Castling

        [TestMethod]
        public void GetLegalMoves_Castling_WhiteKingAndHRookHasNotMoved_WhiteCanCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var castleShort = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, castleShort);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_WhiteKingAndARookHasNotMoved_WhiteCanCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var castleLong = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, castleLong);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_WhiteHRookHasMoved_WhiteCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid);
            chessBoardState = AddStateFlag(chessBoardState, StateFlag.WhiteHRookHasMoved);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD6TakesD7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, pawnD6TakesD7);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_WhiteKingHasMoved_WhiteCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid);
            chessBoardState = AddStateFlag(chessBoardState, StateFlag.WhiteKingHasMoved);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD6TakesD7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, pawnD6TakesD7);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_ShortCastlePathIsCheckedByRook_WhiteCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BR, null, null},
                {null, A.BP, null, A.BP, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, null, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD6TakesD7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, pawnD6TakesD7);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_KingIsChecked_WhiteCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BR, A.BK, null, null},
                {null, A.BP, null, A.BP, null, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, null, null, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD6TakesD7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, pawnD6TakesD7);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_ShortCastleDestinationIsCheckedByRook_WhiteCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, A.BR, null},
                {null, A.BP, null, A.BP, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, null, null, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnD6TakesD7 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.DoesNotContain(chessMoves, pawnD6TakesD7);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_BlackKingAndHRookHasNotMoved_BlackCanCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesShort = new ExecutedChessMove(blackChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, kingCastlesShort);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_BlackHRookHasMoved_BlackCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, A.BP},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid, Player.Black);
            chessBoardState = AddStateFlag(chessBoardState, StateFlag.BlackHRookHasMoved);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesShort = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesShort);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_BlackKingHasMoved_BlackCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, A.BP},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid, Player.Black);
            chessBoardState = AddStateFlag(chessBoardState, StateFlag.BlackKingHasMoved);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesShort = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesShort);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_ShortCastlePathIsCheckedByRook_BlackCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, null, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, A.WR, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesShort = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesShort);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_KingIsChecked_BlackCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, null, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, null, null, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WR, null, null, A.WK}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesShort = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesShort);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_ShortCastleDestinationIsCheckedByRook_BlackCannotCastleShort()
        {
            A?[,] castleShortGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, null, null, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, A.WR, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleShortGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesShort = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesShort);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_WhiteARookHasMoved_WhiteCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BK, A.BB, A.BN, A.BR},
                {null, A.BP, null, A.BP, A.BP, A.BP, A.BP, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, null, null, null, null, A.WP, null, A.WP},
                {null, A.WP, A.WP, null, A.WB, A.WP, A.WP, null},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid);
            chessBoardState = AddStateFlag(chessBoardState, StateFlag.WhiteARookHasMoved);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var castleLong = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, castleLong);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_LongCastlePathIsCheckedByRook_WhiteCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, A.BN, null, A.BQ, A.BK, A.BB, null, null},
                {null, A.BP, null, null, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BR, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, null, null, null, A.WB, null, A.WP, null},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var castleLong = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, castleLong);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_LongCastlePathIsBlocked_WhiteCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, A.BN, null, null, A.BK, A.BB, null, null},
                {null, A.BP, null, null, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, null, null, null, A.WB, null, A.WP, null},
                {A.WR, A.WN, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var castleLong = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, castleLong);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_KingIsChecked_WhiteCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, A.BN, A.BB, A.BQ, A.BR, A.BK, null, null},
                {null, A.BP, null, A.BP, null, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, null, null, A.WP, null},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid);
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var castleLong = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, castleLong);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_LongCastleDestinationIsCheckedByRook_WhiteCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, A.BN, A.BR, A.BQ, A.BK, null, null, null},
                {null, A.BP, null, null, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, null, null, A.WB, null, null, null},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.King, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row1);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var castleLong = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, castleLong);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_BlackKingAndHRookHasNotMoved_BlackCanCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, A.BP},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, null, null, null, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var castleLong = new ExecutedChessMove(blackChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, castleLong);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_BlackHRookHasMoved_BlackCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, A.BP},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.WP, null, null},
                {A.WP, A.WP, A.WP, null, A.WB, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid, Player.Black);
            chessBoardState = AddStateFlag(chessBoardState, StateFlag.BlackARookHasMoved);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesLong = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesLong);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_LongCastlePathIsCheckedByRook_BlackCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, null, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.WR, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, null, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesLong = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesLong);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_LongCastlePathIsBlocked_BlackCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, A.BN, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, null, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, null, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesLong = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesLong);
        }


        [TestMethod]
        public void GetLegalMoves_Castling_KingIsChecked_BlackCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, null, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, null, null, A.WP, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WR, null, null, A.WK}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesLong = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesLong);
        }

        [TestMethod]
        public void GetLegalMoves_Castling_LongCastleDestinationIsCheckedByRook_BlackCannotCastleLong()
        {
            A?[,] castleLongGrid =
            {
                {A.BR, null, null, null, A.BK, null, null, A.BR},
                {null, A.BP, null, A.BP, A.BP, null, null, null},
                {A.BP, null, null, null, null, null, null, null},
                {null, null, null, A.BP, null, null, null, null},
                {null, null, A.WR, null, null, null, null, null},
                {null, null, null, null, null, null, null, A.WP},
                {A.WP, A.WP, A.WP, null, A.WB, null, null, null},
                {A.WR, A.WN, A.WB, A.WQ, A.WK, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(castleLongGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row8);
            var chessPiece = new ChessPiece(POS.King, Player.Black);
            var startPosition = new ChessPosition(COL.E, ROW.Row8);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var kingCastlesLong = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, kingCastlesLong);
        }

        #endregion

        #region Special Situations

        [TestMethod]
        public void GetLegalMoves_StaleMate_NoLegalMoves()
        {
            A?[,] staleMateGrid =
            {
                {null, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(staleMateGrid);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            Assert.IsFalse(chessMoves.Any());
        }

        [TestMethod]
        public void GetLegalMoves_CheckMate_NoLegalMoves()
        {
            A?[,] checkMateGrid =
            {
                {A.BK, null, null, null, null, null, null, A.WK},
                {null, null, null, null, null, A.BN, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.BR, null},
                {null, null, A.WP, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(checkMateGrid);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            Assert.IsFalse(chessMoves.Any());
        }

        [TestMethod]
        public void GetLegalMoves_Promotion_BlackPawnMovesToLastRow_CanBecomeQueen()
        {
            A?[,] staleMateGrid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, A.BP, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(staleMateGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row2);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToQueen);
            var pawnC2ToC1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.Contains(chessMoves, pawnC2ToC1.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_Promotion_BlackPawnMovesToLastRow_CanBecomeKnight()
        {
            A?[,] staleMateGrid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, A.BP, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(staleMateGrid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.C, ROW.Row1);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.C, ROW.Row2);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToKnight);
            var pawnC2ToC1 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.Contains(chessMoves, pawnC2ToC1.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_Promotion_WhitePawnMovesToLastRow_CanBecomeQueen()
        {
            A?[,] staleMateGrid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(staleMateGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.H, ROW.Row8);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.H, ROW.Row7);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToQueen);
            var pawnH7ToH8 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.Contains(chessMoves, pawnH7ToH8.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_Promotion_WhitePawnTakesToLastRow_CanBecomeQueen()
        {
            A?[,] staleMateGrid =
            {
                {null, null, null, null, null, null, A.BB, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(staleMateGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row8);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.H, ROW.Row7);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToQueen);
            var pawnH7TakesG8 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.Contains(chessMoves, pawnH7TakesG8.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_Promotion_WhitePawnMovesToLastRow_CanBecomeKnight()
        {
            A?[,] staleMateGrid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, A.WP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.WK, null},
                {null, null, null, null, null, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(staleMateGrid);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.H, ROW.Row8);
            var chessPiece = new ChessPiece(POS.Pawn, Player.White);
            var startPosition = new ChessPosition(COL.H, ROW.Row7);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition, MoveOption.ConvertPawnToKnight);
            var pawnH7ToH8 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.Contains(chessMoves, pawnH7ToH8.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_DiscoveredCheck_WhiteRookProtectingKingFromBlackRook_CannotMoveRookToSides()
        {
            A?[,] protectedKingGrid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, null},
                {null, null, null, null, A.BR, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WR, null, null, null},
                {null, null, null, null, A.WK, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(protectedKingGrid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.G, ROW.Row2);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var rookE2ToG2 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, rookE2ToG2.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_DiscoveredCheck_CheckedByBlackKnight_CannotMoveImmaterialPiece()
        {
            A?[,] grid =
            {
                {A.BR, null, null, null, null, A.BK, null, null},
                {null, null, null, null, null, A.BP, null, A.BR},
                {null, A.BP, null, null, A.BB, A.BB, null, A.BP},
                {A.BP, null, null, A.WN, null, null, null, A.WP},
                {A.WP, null, A.BN, A.WP, null, null, A.WN, null},
                {null, A.WP, null, A.BN, null, null, A.WP, null},
                {null, A.WB, A.WP, A.WP, A.WB, A.WK, null, null},
                {A.WR, null, A.WQ, null, null, null, A.WR, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            
            
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.F, ROW.Row3);
            var chessPiece = new ChessPiece(POS.Bishop, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var bishopE2ToF3 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, bishopE2ToF3.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_IsInCheckByQueenFromAfar_CannotMoveImmaterialPiece()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, null, A.BK, A.BB, A.BN, A.BR},
                {A.BP, A.BP, A.BP, A.BP, null, A.BP, A.BP, A.BP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.BQ, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, A.WP, A.WQ, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, null, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.A, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.A, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnA7ToA6 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, pawnA7ToA6.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_IsInCheckByQueenFromCloseUp_CannotMoveImmaterialPiece()
        {
            A?[,] grid =
            {
                {A.BR, A.BN, A.BB, null, A.BK, A.BB, A.BN, A.BR},
                {A.BP, A.BP, A.BP, A.BP, A.WQ, A.BP, A.BP, A.BP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, A.BQ, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {A.WP, A.WP, A.WP, A.WP, null, A.WP, A.WP, A.WP},
                {A.WR, A.WN, A.WB, null, A.WK, A.WB, A.WN, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid, Player.Black);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.A, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Pawn, Player.Black);
            var startPosition = new ChessPosition(COL.A, ROW.Row7);
            var blackChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var pawnA7ToA6 = new ExecutedChessMove(blackChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, pawnA7ToA6.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_IsInCheckByQueenFromCloseUp_CannotMoveImmaterialPiece2()
        {
            A?[,] grid =
            {
                {null, A.BN, null, null, A.BK, null, null, A.BR},
                {null, A.BB, null, A.BP, null, A.BP, A.BB, null},
                {A.BR, A.BP, null, null, null, A.BN, A.WQ, null},
                {A.BP, null, null, A.BP, A.BP, null, null, null},
                {A.WN, null, null, A.WP, null, A.BP, null, A.WP},
                {A.WP, null, null, null, A.WP, null, null, null},
                {null, null, A.WP, A.BQ, A.WK, A.WN, A.WP, null},
                {A.WR, null, A.WB, null, null, null, null, A.WR}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(grid);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.F, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Queen, Player.White);
            var startPosition = new ChessPosition(COL.G, ROW.Row6);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var queenG6ToF5 = new ExecutedChessMove(whiteChessMove, new TimeSpan());
            CollectionAssert.DoesNotContain(chessMoves, queenG6ToF5.ChessMove);
        }

        [TestMethod]
        public void GetLegalMoves_DiscoveredCheck_WhiteRookProtectingKingFromBlackRook_CanAttackBlackRook()
        {
            A?[,] protectedKingGrid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, null},
                {null, null, null, null, A.BR, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WR, null, null, null},
                {null, null, null, null, A.WK, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(protectedKingGrid);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.E, ROW.Row6);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var rookE2TakesE6 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, rookE2TakesE6);
        }

        [TestMethod]
        public void GetLegalMoves_DiscoveredCheck_WhiteRookProtectingKingFromBlackRook_CanMoveUpNextToBlackRook()
        {
            A?[,] protectedKingGrid =
            {
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, A.BK, null, null},
                {null, null, null, null, A.BR, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, A.WR, null, null, null},
                {null, null, null, null, A.WK, null, null, null}
            };

            var chessBoardState = _chessCalculationsService.GetStateFromGrid(protectedKingGrid);
            var chessMoves = _chessCalculationsService.GetLegalMoves(chessBoardState);
            var endPosition = new ChessPosition(COL.E, ROW.Row5);
            var chessPiece = new ChessPiece(POS.Rook, Player.White);
            var startPosition = new ChessPosition(COL.E, ROW.Row2);
            var whiteChessMove = new ChessMove(new ChessPiecePosition(chessPiece, startPosition), endPosition);
            var rookE2TakesE5 = new ExecutedChessMove(whiteChessMove, new TimeSpan()).ChessMove;
            CollectionAssert.Contains(chessMoves, rookE2TakesE5);
        }

        #endregion

        private static ChessBoardState AddStateFlag(ChessBoardState state, StateFlag stateFlag)
        {
            return new ChessBoardState(state.ChessGrid, state.WhiteTime, state.BlackTime, state.NextToMove, stateFlag, state.EndResult, state.MoveHistory.ToArray(), state.GridHashHistory.ToArray());
        }

        private static ChessBoardState AddLastMove(ChessBoardState state, ChessMove lastMove)
        {
            return new ChessBoardState(state.ChessGrid, state.WhiteTime, state.BlackTime, state.NextToMove, state.StateFlags, state.EndResult, state.MoveHistory.Concat(new []{new LoggedChessMove(lastMove, "", TimeSpan.Zero)}).ToArray(), state.GridHashHistory.ToArray());
        }
    }
}