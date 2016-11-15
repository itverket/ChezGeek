using System;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamPink.Messages
{
    [Serializable]
    public class EvaluateMove : SerializableMessage
    {
        public EvaluateMove(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves, TimeSpan timePlayed)
        {
            StateAndMove = stateAndMove;
            AvailableStateMoves = availableStateMoves;
            TimeLeft = timePlayed;
        }

        public PreliminaryStateAndMove StateAndMove { get; private set; }
        public AvailableStateMoves AvailableStateMoves { get; private set; }
        public TimeSpan TimeLeft { get; private set; }
    }
}