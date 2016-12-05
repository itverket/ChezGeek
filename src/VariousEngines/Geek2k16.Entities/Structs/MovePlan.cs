using System;
using System.Collections.Generic;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct MovePlan
    {
        public MovePlan(IReadOnlyList<PreliminaryStateAndMove> chainedMoves, float estimatedValue)
        {
            ChainedMoves = chainedMoves ?? new List<PreliminaryStateAndMove>();
            EstimatedValue = estimatedValue;
        }

        public IReadOnlyList<PreliminaryStateAndMove> ChainedMoves { get; private set; }
        public float EstimatedValue { get; private set; }

        public override string ToString()
        {
            return string.Join(" -> ", ChainedMoves) + " => " + EstimatedValue;
        }
    }
}