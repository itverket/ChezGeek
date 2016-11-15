using ChezGeek.Common.Actors;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities;
using System;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class GetInitialGameStateAnswer : SerializableMessage
    {
        public GetInitialGameStateAnswer(ChessBoardState initialChessBoardState)
        {
            InitialChessBoardState = new ChessBoardStateViewModel(initialChessBoardState);
        }

        public ChessBoardStateViewModel InitialChessBoardState { get; private set; }
    }
}
