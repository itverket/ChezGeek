using ChezGeek.Common.Serialization;
using Geek2k16.Entities;
using System;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class GetNextBoardStateAnswer : SerializableMessage
    {
        public GetNextBoardStateAnswer(ChessBoardState state)
        {
            ChessBoardState = state;
        }

        public ChessBoardState ChessBoardState { get; private set; }
    }
}
