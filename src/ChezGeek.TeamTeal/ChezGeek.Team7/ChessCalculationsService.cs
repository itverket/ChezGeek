using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Geek2k16.Common.Extensions;
using Geek2k16.Entities;
using Geek2k16.Entities.Constants;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using System.Collections.Concurrent;

namespace ChezGeek.TeamTeal
{
    public class ChessCalculationsService
    {
        private ConcurrentDictionary<long, float> boardStates = new ConcurrentDictionary<long, float>();

        #region Private Classes

        private class StateWithPositions
        {
            public StateWithPositions(IPreliminaryBoardState state)
            {
                State = state;
                ChessPiecePositions =
                    state.ChessGrid.GetAllChessPiecePositions()
                        .Where(x => x.ChessPiece.Player == State.NextToMove)
                        .ToArray();
            }

            private IPreliminaryBoardState State { get; }
            private ChessPiecePosition[] ChessPiecePositions { get; }

            public StateWithPositionsAndOffsets GetActivePieceOffsets()
            {
                return new StateWithPositionsAndOffsets(State,
                    ChessPiecePositions.ToDictionary(x => x, CalculateOffsets));
            }

            private PositionOffset[] CalculateOffsets(ChessPiecePosition x)
            {
                return
                    LegalMoves.PieceOffsets[x.ChessPiece.PieceType].Select(
                            y => x.ChessPiece.PieceType == PieceType.Pawn ? y.AsPlayer(State.NextToMove) : y)
                        .SelectMany(o => ExtrapolateSquares(o, x))
                        .ToArray();
            }

            private static IEnumerable<PositionOffset> ExtrapolateSquares(PositionOffset offset,
                ChessPiecePosition position)
            {
                yield return offset;
                if (offset.MoveCondition.HasFlag(MoveCondition.DoubleMoveIfStartRow) && IsInStartPosition(position))
                    yield return new PositionOffset(offset.Columns, offset.Rows * 2, offset.MoveCondition);
                if (offset.MoveCondition.HasFlag(MoveCondition.UnlimitedDistance))
                    for (var i = 2; i < 8; i++)
                        yield return new PositionOffset(offset.Columns * i, offset.Rows * i, offset.MoveCondition);
            }

            private static bool IsInStartPosition(ChessPiecePosition position)
            {
                if (position.ChessPiece.PieceType == PieceType.Pawn)
                {
                    var startRow = position.ChessPiece.Player == Player.White ? ChessRow.Row2 : ChessRow.Row7;
                    return startRow == position.ChessPosition.Row;
                }
                return false;
            }
        }

        private class StateWithPositionsAndOffsets
        {
            public StateWithPositionsAndOffsets(IPreliminaryBoardState state,
                Dictionary<ChessPiecePosition, PositionOffset[]> chessPiecePositions)
            {
                State = state;
                ChessPiecePositions = chessPiecePositions;
            }

            private IPreliminaryBoardState State { get; }
            private Dictionary<ChessPiecePosition, PositionOffset[]> ChessPiecePositions { get; }

            private Dictionary<ChessMove, MoveCondition> GetPotentialChessMoves()
            {
                return ChessPiecePositions.SelectMany(x => x.Value.Select(o => new
                {
                    ChessPiecePosition = x.Key,
                    Offset = o,
                    NewPosition = PositionFromOffset(x.Key.ChessPosition, o)
                }))
                    .Where(x => x.NewPosition.HasValue)
                    .Select(x => new { chessMove = new ChessMove(x.ChessPiecePosition, x.NewPosition.Value), x.Offset })
                    .SelectMany(x => ExpandWithPromotionMoves(x.chessMove).Select(y => new { chessMove = y, x.Offset }))
                    .ToDictionary(x => x.chessMove, x => x.Offset.MoveCondition);
            }

            private static IEnumerable<ChessMove> ExpandWithPromotionMoves(ChessMove chessMove)
            {
                var player = chessMove.ChessPiecePosition.ChessPiece.Player;
                if ((chessMove.PieceType == PieceType.Pawn) &&
                    (((player == Player.White) && (chessMove.ToPosition.Row == ChessRow.Row8)) ||
                     ((player == Player.Black) && (chessMove.ToPosition.Row == ChessRow.Row1))))
                    return new[] { MoveOption.ConvertPawnToKnight, MoveOption.ConvertPawnToQueen }
                        .Select(o => new ChessMove(chessMove, o));
                return new[] { chessMove };
            }

            public StateWithPotentialMoves GetPotentialMoves()
            {
                return new StateWithPotentialMoves(State, GetPotentialChessMoves());
            }

            private static ChessPosition? PositionFromOffset(ChessPosition position, PositionOffset offset)
            {
                var columnValue = (int)position.Column + offset.Columns;
                var column = (0 <= columnValue) && (columnValue <= 7) ? (ChessColumn?)columnValue : null;
                var rowValue = (int)position.Row + offset.Rows;
                var row = (0 <= rowValue) && (rowValue <= 7) ? (ChessRow?)rowValue : null;
                if (row.HasValue && column.HasValue)
                    return new ChessPosition(column.Value, row.Value);
                return null;
            }
        }

        private class StateWithPotentialMoves
        {
            public StateWithPotentialMoves(IPreliminaryBoardState state, Dictionary<ChessMove, MoveCondition> potentialMoves)
            {
                State = state;
                PotentialMoves = potentialMoves;
            }

            private IPreliminaryBoardState State { get; }
            private Dictionary<ChessMove, MoveCondition> PotentialMoves { get; }

            public StateWithPotentialMoves FilterBasedOnConditions()
            {
                var enemyPositions = GetEnemyPositions();
                var friendlyPositions = GetFriendlyPositions();

                HandlePawnMustCapture(enemyPositions);
                HandlePawnCannotCapture(enemyPositions);
                FilterLandingOnFriendly(friendlyPositions);
                FilterBlockedByPiece(enemyPositions, friendlyPositions);
                FilterForUnavailableCastling(enemyPositions, friendlyPositions);
                return this;
            }

            public StateWithPotentialMoves FilterCastlingByThreatenedSquares(IPreliminaryBoardState state)
            {
                var longCastling =
                    PotentialMoves.Where(x => x.Value.HasFlag(MoveCondition.CastleLong))
                        .Select(x => x.Key)
                        .Cast<ChessMove?>()
                        .FirstOrDefault();
                var shortCastling =
                    PotentialMoves.Where(x => x.Value.HasFlag(MoveCondition.CastleShort))
                        .Select(x => x.Key)
                        .Cast<ChessMove?>()
                        .FirstOrDefault();
                if (longCastling.HasValue || shortCastling.HasValue)
                {
                    var threatPositions = ChessCalculationsService.GetThreatenedSquares(state, true);
                    if (longCastling.HasValue)
                    {
                        var illegalLongCastling =
                            new[] { longCastling.Value.FromPosition }.Concat(ChessCalculationsService.IntermediatePositions(longCastling.Value))
                                .Intersect(threatPositions)
                                .Any();
                        if (illegalLongCastling)
                            PotentialMoves.Remove(longCastling.Value);
                    }
                    if (shortCastling.HasValue)
                    {
                        var illegalShortCastling =
                            new[] { shortCastling.Value.FromPosition }.Concat(ChessCalculationsService.IntermediatePositions(shortCastling.Value))
                                .Intersect(threatPositions)
                                .Any();
                        if (illegalShortCastling)
                            PotentialMoves.Remove(shortCastling.Value);
                    }
                }
                return this;
            }

            private void FilterForUnavailableCastling(IEnumerable<ChessPosition> enemyPositions,
                IEnumerable<ChessPosition> friendlyPositions)
            {
                var allPositions = enemyPositions.Concat(friendlyPositions).ToArray();
                var longCastling =
                    PotentialMoves.Where(x => x.Value.HasFlag(MoveCondition.CastleLong))
                        .Select(x => x.Key)
                        .Cast<ChessMove?>()
                        .FirstOrDefault();
                var shortCastling =
                    PotentialMoves.Where(x => x.Value.HasFlag(MoveCondition.CastleShort))
                        .Select(x => x.Key)
                        .Cast<ChessMove?>()
                        .FirstOrDefault();

                if (longCastling.HasValue && !LegalLongCastling(allPositions))
                    PotentialMoves.Remove(longCastling.Value);
                if (shortCastling.HasValue && !LegalShortCastling(allPositions))
                    PotentialMoves.Remove(shortCastling.Value);
            }

            private bool LegalShortCastling(IEnumerable<ChessPosition> allPositions)
            {
                var kingsFlag = new Dictionary<Player, StateFlag>
                {
                    {Player.White, StateFlag.WhiteKingHasMoved},
                    {Player.Black, StateFlag.BlackKingHasMoved}
                }[State.NextToMove];
                var hRookFlag = new Dictionary<Player, StateFlag>
                {
                    {Player.White, StateFlag.WhiteHRookHasMoved},
                    {Player.Black, StateFlag.BlackHRookHasMoved}
                }[State.NextToMove];
                if ((State.StateFlags?.HasFlag(kingsFlag) == true) || (State.StateFlags?.HasFlag(hRookFlag) == true))
                    return false;
                var kingsRow = GridConstants.Rules.KingsRows[State.NextToMove];
                var betweenShortCastling =
                    new[] { ChessColumn.F, ChessColumn.G }.Select(c => new ChessPosition(c, kingsRow));
                return !allPositions.Intersect(betweenShortCastling).Any();
            }

            private bool LegalLongCastling(IEnumerable<ChessPosition> allPositions)
            {
                var kingsFlag = new Dictionary<Player, StateFlag>
                {
                    {Player.White, StateFlag.WhiteKingHasMoved},
                    {Player.Black, StateFlag.BlackKingHasMoved}
                }[State.NextToMove];
                var aRookFlag = new Dictionary<Player, StateFlag>
                {
                    {Player.White, StateFlag.WhiteARookHasMoved},
                    {Player.Black, StateFlag.BlackARookHasMoved}
                }[State.NextToMove];
                if ((State.StateFlags?.HasFlag(kingsFlag) == true) || (State.StateFlags?.HasFlag(aRookFlag) == true))
                    return false;
                var kingsRow = GridConstants.Rules.KingsRows[State.NextToMove];
                var betweenLongCastling =
                    new[] { ChessColumn.B, ChessColumn.C, ChessColumn.D }.Select(c => new ChessPosition(c, kingsRow));
                return !allPositions.Intersect(betweenLongCastling).Any();
            }

            private void FilterBlockedByPiece(ChessPosition[] enemyPositions,
                IEnumerable<ChessPosition> friendlyPositions)
            {
                var allPositions = enemyPositions.Concat(friendlyPositions);
                var blockedByPieces =
                    PotentialMoves.Where(x => !x.Value.HasFlag(MoveCondition.MoveThroughPieces))
                        .Where(x => ChessCalculationsService.IntermediatePositions(x.Key).Intersect(allPositions).Any())
                        .Select(x => x.Key)
                        .ToArray();

                foreach (var illegalMove in blockedByPieces)
                    PotentialMoves.Remove(illegalMove);
            }

            private void FilterLandingOnFriendly(ChessPosition[] friendlyPositions)
            {
                var landingOnFriendly =
                    PotentialMoves.Where(x => friendlyPositions.Contains(x.Key.ToPosition)).Select(x => x.Key).ToArray();

                foreach (var illegalMove in landingOnFriendly)
                    PotentialMoves.Remove(illegalMove);
            }

            private void HandlePawnMustCapture(ChessPosition[] enemyPositions)
            {
                var enemyPositionsWithEnPassant = GetEnemyPositionsWithEnPassant(enemyPositions);
                var mustCaptureIllegals =
                    PotentialMoves.Where(x => x.Value.HasFlag(MoveCondition.MustCapture))
                        .Where(x => !enemyPositionsWithEnPassant.Contains(x.Key.ToPosition))
                        .Select(x => x.Key)
                        .ToArray();

                foreach (var illegalMove in mustCaptureIllegals)
                    PotentialMoves.Remove(illegalMove);
            }

            private void HandlePawnCannotCapture(ChessPosition[] enemyPositions)
            {
                var cannotCaptureIllegals =
                    PotentialMoves.Where(x => x.Value.HasFlag(MoveCondition.CannotCapture))
                        .Where(x => enemyPositions.Contains(x.Key.ToPosition))
                        .Select(x => x.Key)
                        .ToArray();

                foreach (var illegalMove in cannotCaptureIllegals)
                    PotentialMoves.Remove(illegalMove);
            }

            private ChessPosition[] GetFriendlyPositions()
            {
                return
                    State.ChessGrid.GetAllChessPiecePositions()
                        .Where(x => x.ChessPiece.Player == State.NextToMove)
                        .Select(x => x.ChessPosition)
                        .ToArray();
            }

            private ChessPosition[] GetEnemyPositions()
            {
                return
                    State.ChessGrid.GetAllChessPiecePositions()
                        .Where(x => x.ChessPiece.Player != State.NextToMove)
                        .Select(x => x.ChessPosition)
                        .ToArray();
            }

            private ChessPosition[] GetEnemyPositionsWithEnPassant(ChessPosition[] enemyPositions)
            {
                if ((State.LastMove?.ChessMove.PieceType != PieceType.Pawn) ||
                    (State.LastMove?.ChessMove.MoveDistance != 2))
                    return enemyPositions;
                return enemyPositions.Concat(ChessCalculationsService.IntermediatePositions(State.LastMove.Value.ChessMove)).ToArray();
            }

            public ChessMove[] ReturnChessMoves()
            {
                return PotentialMoves.Keys.ToArray();
            }
        }

        #endregion

        #region public  

        public ChessBoardState GetInitialState()
        {
            return GetStateFromGrid(GridConstants.InitialGrid);
        }

        public ChessBoardState GetStateFromGrid(Abbr?[,] gridArray, Player player = Player.White)
        {
            var chessGrid = SetupChessGrid(gridArray);
            var stateGridHash = GetGridHash(chessGrid);
            var hashHistory = new List<long> { stateGridHash };
            return new ChessBoardState(chessGrid, RuleConstants.StartTime, RuleConstants.StartTime, player, gridHashHistory: hashHistory);
        }

        public PreliminaryBoardState GetPreliminaryStateFromGrid(Abbr?[,] gridArray, Player player = Player.White)
        {
            return new PreliminaryBoardState(SetupChessGrid(gridArray), player);
        }

        public ChessBoardState GetStateAfterMove(ChessBoardState originalState, ExecutedChessMove executedMove)
        {
            var move = executedMove.ChessMove;
            var newFlag = GridConstants.Rules.PieceFlags.Where(x => x.Key.Equals(move.ChessPiecePosition))
                .Select(x => x.Value).FirstOrDefault();

            var whiteTimeUsed = executedMove.ChessMove.Player == Player.White
                ? executedMove.TimeUsed - RuleConstants.MoveIncrement
                : new TimeSpan();
            var blackTimeUsed = executedMove.ChessMove.Player == Player.Black
                ? executedMove.TimeUsed - RuleConstants.MoveIncrement
                : new TimeSpan();

            var chessGrid = PerformReactiveMove(move, GetGridAfterMove(originalState.ChessGrid, move));
            var stateFlags = newFlag.HasValue ? (originalState.StateFlags | newFlag ?? newFlag.Value) : originalState.StateFlags;
            var whiteTime = originalState.WhiteTime - whiteTimeUsed;
            var blackTime = originalState.BlackTime - blackTimeUsed;
            var player = originalState.NextToMove.Reverse();


            var hashHistory = new List<long>(originalState.GridHashHistory) { GetGridHash(chessGrid) };
            var hasTaken = MoveHasTaken(chessGrid, move);
            var moveHistory = originalState.MoveHistory.Select(x => x.ChessMove).ToList();
            moveHistory.Add(move);
            var result = GetOutOfTimeResult(originalState, executedMove) ??
                         GetIllegalMoveResult(originalState, move) ??
                         GetRepeatThreeTimesResult(hashHistory) ??
                         GetInsufficientMaterialResult(chessGrid) ??
                         GetFiftyMovesResult(moveHistory, hasTaken) ??
                         GetMatingMoveResult(originalState, move);

            var loggedChessMove = CreateLoggedChessMove(originalState, executedMove, result);
            var loggedMoves = new List<LoggedChessMove>(originalState.MoveHistory) { loggedChessMove };

            var endResult = result.HasValue && RuleConstants.WinningPlayer.Keys.Contains(result.Value)
                ? result
                : null;
            return new ChessBoardState(chessGrid, whiteTime, blackTime, player, stateFlags, endResult, loggedMoves, hashHistory);
        }

        public float GetValueOfPieces(ChessBoardState state)
        {
            return GetEndResultValue(state.EndResult) ??
                   GetValueOfPieces(state.ChessGrid);
        }

        public float GetValueOfPieces(ChessGrid grid)
        {
            var peices = grid.GetAllChessPiecePositions().ToList();

            float sum = 0;
            foreach (var peice in peices)
            {
                float evaluatedValue;

                switch (peice.ChessPiece.PieceType)
                {
                    case PieceType.Pawn:
                        evaluatedValue = PieceEvaluator.EvaluatePawn(peices, peice);
                        break;
                    case PieceType.Rook:
                        evaluatedValue = PieceEvaluator.EvaluateRook(peices, peice);
                        break;
                    case PieceType.Bishop:
                        evaluatedValue = PieceEvaluator.EvaluateBishop(peices, peice);
                        break;
                    default:
                        evaluatedValue = Calculations.PieceValues[peice.ChessPiece.PieceType];
                        break;

                }

                sum += evaluatedValue * (int)peice.ChessPiece.Player;
            }
            return sum;
        }

        public string GetMoveHistoryText(ChessBoardState state)
        {
            return string.Join("  ", GetMoveHistoryStrings(state));
        }

        public ChessPosition[] GetIntermediatePositions(ChessMove chessMove)
        {
            return IntermediatePositions(chessMove);
        }

        public ChessMove[] GetLegalMoves(ChessBoardState state)
        {
            var availableMoves = GetAvailableMoves(state);
            var availableMovesWithNextPly = GetAvailableMovesWithNextPlyOfAvailableMoves(state, availableMoves);
            return FilterBySelfMatingMoves(availableMovesWithNextPly, state);
        }

        public ChessMove[] GetAvailableMoves(IPreliminaryBoardState state)
        {
            return new ChessCalculationsService.StateWithPositions(state).GetActivePieceOffsets()
                .GetPotentialMoves()
                .FilterBasedOnConditions()
                .FilterCastlingByThreatenedSquares(state)
                .ReturnChessMoves();
        }

        public AvailableStateMoves GetCurrentAvailableStateMoves(PreliminaryBoardState state)
        {
            var moves = GetAvailableMoves(state);
            return new AvailableStateMoves(state, moves);
        }

        public Dictionary<PreliminaryStateAndMove, AvailableStateMoves> GetAvailableMovesWithNextPlyOfAvailableMoves(AvailableStateMoves availableStateMoves)
        {
            return availableStateMoves.NextMoves.ToDictionary(
                x => new PreliminaryStateAndMove(availableStateMoves.State, x),
                x => GetNextAvailableStateMoves(availableStateMoves.State, x));
        }

        public AvailableStateMoves GetNextAvailableStateMoves(IPreliminaryBoardState originalState, ChessMove nextMove, IReadOnlyList<PreliminaryStateAndMove> prependedMoves = null)
        {
            var nextState = CreateNewPreliminaryState(originalState, nextMove);
            var nextMoves = GetAvailableMovesIgnoreCastlingFiltering(nextState);
            return new AvailableStateMoves(nextState, nextMoves, prependedMoves);
        }

        public Dictionary<ChessMove, ChessMove[]> GetAvailableMovesWithNextPlyOfAvailableMoves(ChessBoardState state, ChessMove[] availableMoves = null)
        {
            return (availableMoves ?? GetAvailableMoves(state)).ToDictionary(x => x,
                x => GetAvailableMovesIgnoreCastlingFiltering(CreateNewState(state, x)));
        }

        public ChessMove[] FilterBySelfMatingMoves(Dictionary<ChessMove, ChessMove[]> chessMovesWithNextPly, IPreliminaryBoardState initialState)
        {
            var selfMatingMoves =
                chessMovesWithNextPly.Where(x => IsSelfMatingMove(x.Key, x.Value, initialState))
                    .Select(x => x.Key)
                    .ToArray();
            return chessMovesWithNextPly.Keys.Except(selfMatingMoves).ToArray();
        }

        public AvailableStateMoves FilterToLegalStateMoves(Dictionary<PreliminaryStateAndMove, AvailableStateMoves> availableStateMovesDictionary, AvailableStateMoves availableStateMoves)
        {
            var movesDictionary = availableStateMovesDictionary.ToDictionary(x => x.Key.NextMove, x => x.Value.NextMoves.ToArray());
            var legalMoves = FilterBySelfMatingMoves(movesDictionary, availableStateMoves.State);
            return new AvailableStateMoves(availableStateMoves, legalMoves);
        }

        public ChessGrid GetGridAfterMove(ChessGrid originalGrid, ChessMove move)
        {
            var emptyGrid = new ChessPiece?[8, 8];
            var reactiveMove = GridConstants.Rules.ReactiveMoves
                .Where(x => x.Key.Equals(move)).Select(x => x.Value).FirstOrDefault();
            var moves = new[] { move, reactiveMove }.OfType<ChessMove>().ToArray();

            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var chessColumn = (ChessColumn)column;
                    var chessRow = (ChessRow)row;
                    var chessPosition = new ChessPosition(chessColumn, chessRow);

                    if (moves.Any(x => x.ChessPiecePosition.ChessPosition == chessPosition))
                        continue;

                    var chessPiece = move.ChessPiecePosition.ChessPiece;
                    var player = chessPiece.Player;

                    if ((chessPiece.PieceType == PieceType.Pawn) && move.MoveOptions.HasValue &&
                        (chessRow == GridConstants.Rules.PromotionRows[player]))
                        chessPiece = new ChessPiece(GridConstants.Rules.PawnPromotion[move.MoveOptions.Value], player);

                    var piece = move.ToPosition == chessPosition ? chessPiece : originalGrid[chessColumn, chessRow];

                    if (!piece.HasValue)
                        continue;

                    emptyGrid[(int)chessColumn, (int)chessRow] = piece.Value;
                }
            return new ChessGrid(emptyGrid);
        }

        public MovePlan[] GetBestPlans(AvailableStateMoves thisPlyStateMoves, Dictionary<PreliminaryStateAndMove, MovePlan> thisPlyToNextPlyEvaluations, int numberOfMoves)
        {
            var state = thisPlyStateMoves.State;
            var nextToMove = state.NextToMove;
            var moveplans = thisPlyStateMoves.NextMoves
                .Select(m => new PreliminaryStateAndMove(state, m))
                .Select(m => CreateMovePlan(m, thisPlyToNextPlyEvaluations[m])).ToArray();

            var maxValue = moveplans.Max(x => x.EstimatedValue * (int)nextToMove);

            return moveplans.Where(x => x.EstimatedValue * (int)nextToMove + 1 >= maxValue).ToArray();
        }

        public MovePlan[] GetBestPlans(AvailableStateMoves leafStateMoves, int moveCount)
        {
            var state = leafStateMoves.State;
            var nextToMove = state.NextToMove;
            var moveplans = leafStateMoves.NextMoves
                .Select(m => new PreliminaryStateAndMove(state, m))
                .Select(x => CreateMovePlan(x, moveCount)).ToArray();


            var maxValue = moveplans.Max(x => x.EstimatedValue * (int)nextToMove);

            return moveplans.Where(x => x.EstimatedValue * (int)nextToMove + 1 >= maxValue).ToArray();
        }

        #endregion

        #region private

        private static MovePlan CreateMovePlan(PreliminaryStateAndMove stateAndMove, MovePlan plan)
        {
            var chainedMoves = new[] { stateAndMove }.Concat(plan.ChainedMoves).ToArray();
            return new MovePlan(chainedMoves, plan.EstimatedValue);
        }

        private MovePlan CreateMovePlan(PreliminaryStateAndMove preliminaryStateAndMove, int moveCount)
        {
            var estimatedValue = EvaluateMove(preliminaryStateAndMove.PreliminaryState.ChessGrid,
                preliminaryStateAndMove.NextMove, moveCount);

            return new MovePlan(new[] { preliminaryStateAndMove }, estimatedValue);
        }

        private float EvaluateMove(ChessGrid chessGrid, ChessMove move, int moveCount)
        {
            var newGrid = GetGridAfterMove(chessGrid, move);

            float calculatedPossition;
            var hashCode = newGrid.GetHashCode();
            if (boardStates.TryGetValue(hashCode, out calculatedPossition))
            {
                return calculatedPossition;
            }

            var value = GetValueOfPieces(newGrid);
            boardStates.TryAdd(hashCode, value);

            return value;

        }


        private StateResult? GetIllegalMoveResult(ChessBoardState originalState, ChessMove move)
        {
            if (GetLegalMoves(originalState).Contains(move))
            {
                return null;
            }
            return new Dictionary<Player, StateResult>
            {
                {Player.White, StateResult.WhiteIllegalMove},
                {Player.Black, StateResult.BlackIllegalMove}
            }[originalState.NextToMove];
        }

        private static bool MoveHasTaken(ChessGrid chessGrid, ChessMove chessMove)
        {
            return chessGrid.GetAllChessPiecePositions().Any(x => x.ChessPosition == chessMove.ToPosition);
        }

        private static ChessMove[] GetAvailableMovesIgnoreCastlingFiltering(IPreliminaryBoardState state)
        {
            return new ChessCalculationsService.StateWithPositions(state)
                .GetActivePieceOffsets()
                .GetPotentialMoves()
                .FilterBasedOnConditions()
                .ReturnChessMoves();
        }

        private static float? GetEndResultValue(StateResult? stateResult)
        {
            if (!stateResult.HasValue || !RuleConstants.WinningPlayer.ContainsKey(stateResult.Value))
                return null;
            var player = RuleConstants.WinningPlayer[stateResult.Value];
            if (player.HasValue)
                return (int)player * 1000;
            return 0;
        }

        private static ChessPosition? GetKingPosition(ChessGrid chessGrid, Player player)
        {
            return chessGrid.GetAllChessPiecePositions()
                .Where(x => (x.ChessPiece.Player == player) && (x.ChessPiece.PieceType == PieceType.King))
                .Select(x => x.ChessPosition)
                .FirstOrDefault();
        }

        private static ChessBoardState SwitchStartingPlayer(ChessBoardState originalState)
        {
            return new ChessBoardState(originalState.ChessGrid, originalState.WhiteTime, originalState.BlackTime, originalState.NextToMove.Reverse(), originalState.StateFlags);
        }

        private static PreliminaryBoardState SwitchStartingPlayerPreliminary(IPreliminaryBoardState originalState)
        {
            return new PreliminaryBoardState(originalState.ChessGrid, originalState.NextToMove.Reverse(), originalState.StateFlags);
        }

        private static ChessGrid SetupChessGrid(Abbr?[,] abbrGrid)
        {
            var chessGrid = new ChessPiece?[8, 8];
            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var piece = abbrGrid[row, column];
                    if (!piece.HasValue)
                        continue;
                    chessGrid[column, row] = Converters.ChessPieces[piece.Value];
                }
            return new ChessGrid(chessGrid);
        }

        private ChessBoardState CreateNewState(ChessBoardState originalState, ChessMove chessMove)
        {
            return CreateNewState(originalState, new ExecutedChessMove(chessMove, TimeSpan.Zero));
        }

        private PreliminaryBoardState CreateNewPreliminaryState(IPreliminaryBoardState originalState, ChessMove move)
        {
            var newFlag = GridConstants.Rules.PieceFlags.Where(x => x.Key.Equals(move.ChessPiecePosition))
                .Select(x => x.Value).FirstOrDefault();
            var chessGrid = PerformReactiveMove(move, GetGridAfterMove(originalState.ChessGrid, move));
            var stateFlags = newFlag.HasValue ? (originalState.StateFlags | newFlag ?? newFlag.Value) : originalState.StateFlags;
            var player = originalState.NextToMove.Reverse();
            return new PreliminaryBoardState(chessGrid, player, stateFlags);
        }

        private ChessBoardState CreateNewState(ChessBoardState originalState, ExecutedChessMove executedMove)
        {
            var move = executedMove.ChessMove;
            var newFlag = GridConstants.Rules.PieceFlags.Where(x => x.Key.Equals(move.ChessPiecePosition))
                .Select(x => x.Value).FirstOrDefault();

            var whiteTimeUsed = executedMove.ChessMove.Player == Player.White
                ? executedMove.TimeUsed - RuleConstants.MoveIncrement
                : new TimeSpan();
            var blackTimeUsed = executedMove.ChessMove.Player == Player.Black
                ? executedMove.TimeUsed - RuleConstants.MoveIncrement
                : new TimeSpan();

            var chessGrid = PerformReactiveMove(move, GetGridAfterMove(originalState.ChessGrid, move));
            var stateFlags = newFlag.HasValue ? (originalState.StateFlags | newFlag ?? newFlag.Value) : originalState.StateFlags;
            var whiteTime = originalState.WhiteTime - whiteTimeUsed;
            var blackTime = originalState.BlackTime - blackTimeUsed;
            var player = originalState.NextToMove.Reverse();
            return new ChessBoardState(chessGrid, whiteTime, blackTime, player, stateFlags);
        }

        private static LoggedChessMove CreateLoggedChessMove(ChessBoardState originalState,
            ExecutedChessMove executedMove, StateResult? stateResult)
        {
            var chessMove = executedMove.ChessMove;
            if ((chessMove.PieceType == PieceType.King) && (chessMove.MoveDistance == 2))
            {
                var caption = chessMove.ToPosition.Column == ChessColumn.C ? "0-0-0" : "0-0";
                return new LoggedChessMove(executedMove, caption, stateResult);
            }
            var enemyPositions = originalState.ChessGrid.GetAllChessPiecePositions()
                .Where(x => x.ChessPiece.Player != originalState.NextToMove)
                .Select(x => x.ChessPosition);
            var takesEnemyPiece = enemyPositions.Any(x => x.Equals(chessMove.ToPosition));
            var takes = takesEnemyPiece ? "x" : "";

            var from = takesEnemyPiece && (chessMove.PieceType == PieceType.Pawn)
                ? StringConstants.ColumnNotation[chessMove.FromPosition.Column]
                : "";

            var promotion = chessMove.MoveOptions == MoveOption.ConvertPawnToQueen
                ? "=Q"
                : (chessMove.MoveOptions == MoveOption.ConvertPawnToKnight ? "=N" : "");

            var result = stateResult.HasValue ? StringConstants.ResultNotation[stateResult.Value] : "";
            var pos = StringConstants.PieceNotation[chessMove.PieceType];
            var col = StringConstants.ColumnNotation[chessMove.ToPosition.Column];
            var row = StringConstants.RowNotation[chessMove.ToPosition.Row];

            var chessMoveCaption = $"{pos}{from}{takes}{col}{row}{promotion}{result}";

            return new LoggedChessMove(executedMove, chessMoveCaption, stateResult);
        }

        private static Player? PlayerOutOfTime(ChessBoardState originalState, ExecutedChessMove executedMove)
        {
            var originalTime = new Dictionary<Player, TimeSpan>
            {
                {Player.White, originalState.WhiteTime },
                {Player.Black, originalState.BlackTime }
            }[originalState.NextToMove];
            return originalTime - executedMove.TimeUsed + RuleConstants.MoveIncrement <= TimeSpan.Zero
                    ? originalState.NextToMove
                    : (Player?)null;
        }

        private static StateResult? GetOutOfTimeResult(ChessBoardState orignalState, ExecutedChessMove executedMove)
        {
            var playerOutOfTime = PlayerOutOfTime(orignalState, executedMove);
            var outOfTime = new Dictionary<Player, StateResult>
            {
                {Player.White, StateResult.WhiteIsOutOfTime },
                {Player.Black, StateResult.BlackIsOutOfTime }
            };
            if (playerOutOfTime.HasValue && outOfTime.ContainsKey(playerOutOfTime.Value))
            {
                return outOfTime[playerOutOfTime.Value];
            }
            return null;
        }

        private static StateResult? GetRepeatThreeTimesResult(IReadOnlyCollection<long> hashHistory)
        {
            return hashHistory.GroupBy(x => x).Any(x => x.Count() > 2)
                ? (StateResult?)StateResult.RepeatStateThreeTimes
                : null;
        }

        private static StateResult? GetInsufficientMaterialResult(ChessGrid chessGrid)
        {
            var positionsLeft = chessGrid.GetAllChessPiecePositions()
    .Where(x => x.ChessPiece.PieceType != PieceType.King).ToArray();
            var piecesLeft = positionsLeft.Select(x => x.ChessPiece).ToArray();
            var insufficientTypes = new[] { PieceType.Knight, PieceType.Bishop };
            var onlyKings = !piecesLeft.Any();
            var onlyOneKnightOrBishop = (piecesLeft.Length == 1) &&
                                        insufficientTypes.Contains(piecesLeft.Single().PieceType);
            var onlySameColouredBishops = (piecesLeft.Length == 2) &&
                                          piecesLeft.All(x => x.PieceType == PieceType.Bishop) &&
                                          (piecesLeft.Select(x => x.Player).Distinct().Count() == 2) &&
                                          (positionsLeft
                                               .Select(x => (int)x.ChessPosition.Row + (int)x.ChessPosition.Column)
                                               .Select(x => x % 2).Distinct().Count() == 1);
            return onlyKings || onlyOneKnightOrBishop || onlySameColouredBishops
                ? (StateResult?)StateResult.InsufficientMaterial
                : null;
        }

        private static StateResult? GetFiftyMovesResult(IReadOnlyCollection<ChessMove> moveHistory, bool hasTaken)
        {
            var lastFiftyMoves = moveHistory.Skip(Math.Max(0, moveHistory.Count - 50));
            return lastFiftyMoves.Count(x => (x.PieceType != PieceType.Pawn) && !hasTaken) ==
                   50
                ? (StateResult?)StateResult.FiftyInconsequentialMoves
                : null;
        }

        private StateResult? GetMatingMoveResult(ChessBoardState previousState, ChessMove move)
        {
            var newState = CreateNewState(previousState, move);
            var player = newState.NextToMove;
            var availableMoves = GetAvailableMoves(newState);
            var availableMovesWithNextPly = GetAvailableMovesWithNextPlyOfAvailableMoves(newState, availableMoves);
            var legalMoves = FilterBySelfMatingMoves(availableMovesWithNextPly, newState);
            var hasLegalMoves = legalMoves.Any();
            var kingIsChecked = KingIsChecked(newState, player, true);
            if (hasLegalMoves && kingIsChecked)
                return player == Player.Black ? StateResult.BlackKingChecked : StateResult.WhiteKingChecked;
            if (hasLegalMoves)
                return null;
            if (!kingIsChecked)
                return StateResult.Stalemate;
            return player == Player.Black
                ? StateResult.BlackKingCheckmated
                : StateResult.WhiteKingCheckmated;
        }

        private ChessGrid PerformReactiveMove(ChessMove move, ChessGrid currentGrid)
        {
            var reactiveMove = GridConstants.Rules.ReactiveMoves
                .Where(x => x.Key.Equals(move)).Select(x => x.Value).FirstOrDefault();
            return reactiveMove.HasValue ? GetGridAfterMove(currentGrid, reactiveMove.Value) : currentGrid;
        }

        private static long GetGridHash(Abbr?[,] gridArray)
        {
            long hc = gridArray.Length;

            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var abbr = gridArray[row, column];
                    var cellValue = abbr.HasValue ? (int)abbr : -1;
                    hc = unchecked(hc * 314159 + cellValue);
                }

            return hc;
        }

        private static long GetGridHash(ChessGrid chessGrid)
        {
            return GetGridHash(GetAbbreviationGrid(chessGrid));
        }

        private static Abbr?[,] GetAbbreviationGrid(ChessGrid chessGrid)
        {
            var dictionary = Converters.ChessPieces.ToDictionary(x => x.Value, x => x.Key);
            var abbrGrid = new Abbr?[8, 8];
            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var piece = chessGrid[(ChessColumn)column, (ChessRow)row];
                    if (!piece.HasValue)
                        continue;
                    abbrGrid[row, column] = dictionary[piece.Value];
                }
            return abbrGrid;
        }

        private static IEnumerable<string> GetMoveHistoryStrings(ChessBoardState state)
        {
            var roundNumber = 1;
            foreach (var ecm in state.MoveHistory)
                if (ecm.ChessMove.Player == Player.White)
                {
                    yield return $"{roundNumber}. {ecm.Caption}";
                }
                else
                {
                    roundNumber++;
                    yield return ecm.Caption;
                }
        }

        private static ChessPosition[] IntermediatePositions(ChessMove chessMove)
        {
            if (chessMove.PieceType == PieceType.Knight) return new ChessPosition[0];

            var fromPoint = new Point((int)chessMove.FromPosition.Column, (int)chessMove.FromPosition.Row);
            var toPoint = new Point((int)chessMove.ToPosition.Column, (int)chessMove.ToPosition.Row);
            var moveVector = Point.Subtract(toPoint, fromPoint);
            var steps = chessMove.MoveDistance;
            var stepVector = moveVector / steps;

            return
                Enumerable.Range(1, steps - 1)
                    .Select(x => Point.Add(fromPoint, stepVector * x))
                    .Select(p => new ChessPosition((ChessColumn)p.X, (ChessRow)p.Y))
                    .ToArray();
        }

        private bool IsSelfMatingMove(ChessMove chessMove, ChessMove[] nextPlyMoves, IPreliminaryBoardState initialState)
        {
            var stateAfterMove = CreateNewPreliminaryState(initialState, chessMove);
            return KingIsChecked(stateAfterMove, nextPlyMoves, initialState.NextToMove);
        }

        private static bool KingIsChecked(IPreliminaryBoardState stateAfterMove, Player player, bool opposingPlayer = false)
        {
            var kingPosition = GetKingPosition(stateAfterMove.ChessGrid, player);
            var threatenedSquares = GetThreatenedSquares(stateAfterMove, opposingPlayer);
            return threatenedSquares.Any(x => x == kingPosition);
        }

        private static bool KingIsChecked(IPreliminaryBoardState stateAfterMove, ChessMove[] nextPlyMoves, Player player)
        {
            var kingPosition = GetKingPosition(stateAfterMove.ChessGrid, player);
            var threatenedSquares = nextPlyMoves.Select(x => x.ToPosition).ToArray();
            return threatenedSquares.Any(x => x == kingPosition);
        }

        private static ChessPosition[] GetThreatenedSquares(IPreliminaryBoardState state, bool opposingPlayer = false)
        {
            var chessBoardState = opposingPlayer ? SwitchStartingPlayerPreliminary(state) : state;
            return new ChessCalculationsService.StateWithPositions(chessBoardState).GetActivePieceOffsets()
                .GetPotentialMoves()
                .FilterBasedOnConditions()
                .ReturnChessMoves()
                .Select(x => x.ToPosition)
                .ToArray();
        }

        #endregion
    }
}