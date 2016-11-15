using System;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.Common.Actors._examples
{
    [Serializable]
    public class MultiPlyerWorkerQuestion : SerializableMessage
    {
        public MultiPlyerWorkerQuestion(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves)
        {
            StateAndMove = stateAndMove;
            AvailableStateMoves = availableStateMoves;
        }

        public PreliminaryStateAndMove StateAndMove { get; private set; }
        public AvailableStateMoves AvailableStateMoves { get; private set; }
    }
}