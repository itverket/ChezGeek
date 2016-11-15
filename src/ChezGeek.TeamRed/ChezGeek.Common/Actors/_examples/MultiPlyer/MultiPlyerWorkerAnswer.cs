using System;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.Common.Actors._examples
{
    [Serializable]
    public class MultiPlyerWorkerAnswer : SerializableMessage
    {
        public MultiPlyerWorkerAnswer(PreliminaryStateAndMove stateAndMove, MovePlan movePlan)
        {
            StateAndMove = stateAndMove;
            MovePlan = movePlan;
        }

        public PreliminaryStateAndMove StateAndMove { get; private set; } 
        public MovePlan MovePlan { get; private set; }
    }
}