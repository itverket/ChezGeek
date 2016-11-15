using System;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamPurple.Messages
{
    [Serializable]
    public class PurplePlyerPlusWorkerQuestion : SerializableMessage
    {
        public PurplePlyerPlusWorkerQuestion(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves, bool isLeafNode = false)
        {
            StateAndMove = stateAndMove;
            AvailableStateMoves = availableStateMoves;
            IsLeafNode = isLeafNode;
        }

        public PreliminaryStateAndMove StateAndMove { get; private set; }
        public AvailableStateMoves AvailableStateMoves { get; private set; }
        public bool IsLeafNode { get; private set; }
    }
}