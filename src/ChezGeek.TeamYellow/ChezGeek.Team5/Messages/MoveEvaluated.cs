using System;

using ChezGeek.Common.Serialization;

using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamYellow.Messages
{
    [Serializable]
    public class MoveEvaluated : SerializableMessage
    {
        public MoveEvaluated(PreliminaryStateAndMove stateAndMove, MovePlan movePlan)
        {
            StateAndMove = stateAndMove;
            MovePlan = movePlan;
        }

        public PreliminaryStateAndMove StateAndMove { get; private set; }

        public MovePlan MovePlan { get; private set; }
    }
}