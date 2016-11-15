using System;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamTeal.Messages
{
    [Serializable]
    public class EvaluateMove : SerializableMessage
    {
        public EvaluateMove(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves, int numberOfMoves)
        {
            StateAndMove = stateAndMove;
            AvailableStateMoves = availableStateMoves;
            NumberOfMoves = numberOfMoves;
        }

        public PreliminaryStateAndMove StateAndMove { get; private set; }
        public AvailableStateMoves AvailableStateMoves { get; private set; }
        public int NumberOfMoves { get; set; }
    }
}