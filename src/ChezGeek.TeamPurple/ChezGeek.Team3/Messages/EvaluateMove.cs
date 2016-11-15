using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamPurple.Messages
{
    [Serializable]
    public class EvaluateMove : SerializableMessage
    {
        public EvaluateMove(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves)
        {
            StateAndMove = stateAndMove;
            AvailableStateMoves = availableStateMoves;
        }

        public PreliminaryStateAndMove StateAndMove { get; private set; }
        public AvailableStateMoves AvailableStateMoves { get; private set; }
    }
}
