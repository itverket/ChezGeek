using System;
using System.Collections.Generic;
using System.Linq;
using Geek2k16.Entities.Constants;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace Geek2k16.Entities
{
    [Serializable]
    public struct ChessBoardState : IPreliminaryBoardState
    {
        public ChessBoardState(ChessGrid chessGrid, TimeSpan whiteTime, TimeSpan blackTime, Player nextToMove = Player.White, StateFlag? stateFlags = null, StateResult? endResult = null, IReadOnlyCollection<LoggedChessMove> moveHistory = null, IReadOnlyCollection<long> gridHashHistory = null)
        {
            StateFlags = stateFlags;
            EndResult = endResult;
            ChessGrid = chessGrid;
            WhiteTime = whiteTime;
            BlackTime = blackTime;
            NextToMove = nextToMove;
            MoveHistory = moveHistory ?? new List<LoggedChessMove>();
            GridHashHistory = gridHashHistory ?? new List<long>();
        }

        public StateFlag? StateFlags { get; private set; }
        public StateResult? EndResult { get; private set; }
        public ChessGrid ChessGrid { get; private set; }
        public TimeSpan WhiteTime { get; private set; }
        public TimeSpan BlackTime { get; private set; }
        public Player NextToMove { get; private set; }
        public LoggedChessMove? LastMove => MoveHistory.Cast<LoggedChessMove?>().LastOrDefault();
        public IReadOnlyCollection<LoggedChessMove> MoveHistory { get; private set; }
        public IReadOnlyCollection<long> GridHashHistory { get; private set; }
        public Player? PlayerWon => EndResult.HasValue ? RuleConstants.WinningPlayer[EndResult.Value] : null;

        public override string ToString()
        {
            return ChessGrid.ToString();
        }
    }
}