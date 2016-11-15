using System;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamBrown.Actors.MultiPlyer
{
    [Serializable]
    public class MultiPlyerPlusWorkerQuestion : SerializableMessage
    {
        public MultiPlyerPlusWorkerQuestion(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves, bool isLeafNode = false)
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