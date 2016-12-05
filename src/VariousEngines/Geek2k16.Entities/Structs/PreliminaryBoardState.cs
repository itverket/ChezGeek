using System;
using Geek2k16.Entities.Enums;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct PreliminaryBoardState : IPreliminaryBoardState
    {
        public PreliminaryBoardState(ChessBoardState chessBoardState)
        {
            StateFlags = chessBoardState.StateFlags;
            ChessGrid = chessBoardState.ChessGrid;
            NextToMove = chessBoardState.NextToMove;
            LastMove = chessBoardState.LastMove;
        }

        public PreliminaryBoardState(ChessGrid chessGrid, Player nextToMove = Player.White, StateFlag? stateFlags = null, LoggedChessMove? loggedChessMove = null)
        {
            StateFlags = stateFlags;
            ChessGrid = chessGrid;
            NextToMove = nextToMove;
            LastMove = loggedChessMove;
        }

        public StateFlag? StateFlags { get; private set; }
        public ChessGrid ChessGrid { get; private set; }
        public Player NextToMove { get; private set; }
        public LoggedChessMove? LastMove { get; private set; }

        public override string ToString()
        {
            return ChessGrid.ToString();
        }
    }
}