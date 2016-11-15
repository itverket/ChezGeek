using ChezGeek.Common.Serialization;
using Geek2k16.Entities;
using Geek2k16.Entities.Structs;
using System;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class IsLegalMoveQuestion : SerializableMessage
    {
        public IsLegalMoveQuestion(ChessBoardState chessBoardState, ChessMove chessMove)
        {
            ChessBoardState = chessBoardState;
            ChessMove = chessMove;
        }

        public ChessBoardState ChessBoardState { get; private set; }
        public ChessMove ChessMove { get; private set; }
    }
}
