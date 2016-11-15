using System;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct PreliminaryStateAndMove
    {
        public PreliminaryStateAndMove(PreliminaryBoardState preliminaryState, ChessMove nextMove)
        {
            NextMove = nextMove;
            PreliminaryState = preliminaryState;
        }

        public PreliminaryBoardState PreliminaryState { get; private set; }
        public ChessMove NextMove { get; private set; }

        public override string ToString()
        {
            return NextMove.ToString();
        }
    }
}