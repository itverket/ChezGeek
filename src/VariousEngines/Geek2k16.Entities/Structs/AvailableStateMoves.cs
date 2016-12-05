using System;
using System.Collections.Generic;
using System.Linq;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct AvailableStateMoves
    {
        public AvailableStateMoves(AvailableStateMoves availableStateMoves, IReadOnlyCollection<ChessMove> legalMoves)
        {
            State = availableStateMoves.State;
            PrependedMoves = availableStateMoves.PrependedMoves;
            NextMoves = legalMoves?.ToArray() ?? new ChessMove[0];
        }

        public AvailableStateMoves(PreliminaryBoardState state, IReadOnlyCollection<ChessMove> availableMoves, IReadOnlyList<PreliminaryStateAndMove> prependedMoves = null)
        {
            State = state;
            NextMoves = availableMoves?.ToArray() ?? new ChessMove[0];
            PrependedMoves = prependedMoves?.ToList() ?? new List<PreliminaryStateAndMove>();
        }

        public PreliminaryBoardState State { get; private set; }
        public IReadOnlyList<PreliminaryStateAndMove> PrependedMoves { get; private set; }
        public IReadOnlyCollection<ChessMove> NextMoves { get; private set; }
    }
}