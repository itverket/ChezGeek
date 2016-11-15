using ChezGeek.Common.Serialization;
using Geek2k16.Entities;
using Geek2k16.Entities.Structs;
using System;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class GetNextBoardStateQuestion : SerializableMessage
    {
        public GetNextBoardStateQuestion(ChessBoardState state, ExecutedChessMove move)
        {
            ExecutedChessMove = move;
            ChessBoardState = state;
        }

        public ChessBoardState ChessBoardState { get; private set; }
        public ExecutedChessMove ExecutedChessMove { get; private set; }
    }
}
