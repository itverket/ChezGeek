using System;
using System.Collections.Generic;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamBrown.Actors.MultiPlyer
{
    [Serializable]
    public class MultiPlyerPlusWorkerAnswer : SerializableMessage
    {
        public MultiPlyerPlusWorkerAnswer(string exceptionMessage)
        {
            ExceptionMessage = exceptionMessage;
        }

        public MultiPlyerPlusWorkerAnswer(MultiPlyerPlusWorkerQuestion question, MovePlan movePlan,
            IReadOnlyDictionary<PreliminaryStateAndMove, AvailableStateMoves> fromPreviousPlyToThisPly)
        {
            StateAndMove = question.StateAndMove;
            AvailableStateMoves = question.IsLeafNode ? new AvailableStateMoves() : question.AvailableStateMoves;
            MovePlan = movePlan;
            FromPreviousPlyToThisPly = question.IsLeafNode ? null : fromPreviousPlyToThisPly;
        }

        public PreliminaryStateAndMove StateAndMove { get; private set; }
        public AvailableStateMoves AvailableStateMoves { get; private set; }

        public IReadOnlyDictionary<PreliminaryStateAndMove, AvailableStateMoves> FromPreviousPlyToThisPly { get;
            private set; }

        public MovePlan MovePlan { get; private set; }
        public string ExceptionMessage { get; private set; }
    }
}