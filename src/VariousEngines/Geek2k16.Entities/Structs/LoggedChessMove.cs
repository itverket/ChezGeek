using System;
using Geek2k16.Entities.Enums;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct LoggedChessMove
    {
        public LoggedChessMove(ChessMove chessMove, string caption, TimeSpan timeUsed, StateResult? result = null)
        {
            ChessMove = chessMove;
            Caption = caption;
            TimeUsed = timeUsed;
            Result = result;
        }

        public LoggedChessMove(ExecutedChessMove chessMove, string caption, StateResult? result = null)
        {
            ChessMove = chessMove.ChessMove;
            Caption = caption;
            Result = result;
            TimeUsed = chessMove.TimeUsed;
        }

        public ChessMove ChessMove { get; private set; }
        public TimeSpan TimeUsed { get; private set; }
        public StateResult? Result { get; private set; }
        public string Caption { get; private set; }
    }
}