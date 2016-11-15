using System;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct ExecutedChessMove
    {
        public ExecutedChessMove(ChessMove chessMove, TimeSpan timeUsed)
        {
            ChessMove = chessMove;
            TimeUsed = timeUsed;
        }

        public ChessMove ChessMove { get; private set; }
        public TimeSpan TimeUsed { get; private set; }
    }
}