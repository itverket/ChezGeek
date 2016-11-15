using ChezGeek.Common.Actors;
using ChezGeek.Common.Serialization;
using System;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class GetNextGameStateAnswer : SerializableMessage
    {
        public GetNextGameStateAnswer(ChessBoardStateViewModel chessBoardState)
        {
            ChessBoardState = chessBoardState;
        }

        public ChessBoardStateViewModel ChessBoardState { get; private set; }
    }
}
