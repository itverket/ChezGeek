using ChezGeek.Common.Serialization;
using Geek2k16.Entities;
using System;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class GetInitialBoardStateAnswer : SerializableMessage
    {
        public GetInitialBoardStateAnswer(ChessBoardState initialBoardState)
        {
            InitialBoardState = initialBoardState;
        }

        public ChessBoardState InitialBoardState { get; private set; }
    }
}
