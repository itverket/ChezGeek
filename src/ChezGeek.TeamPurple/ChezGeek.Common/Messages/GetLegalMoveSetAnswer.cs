using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;
using System;
using System.Collections.Generic;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class GetLegalMoveSetAnswer : SerializableMessage
    {
        public GetLegalMoveSetAnswer(ChessMove[] legalMoves)
        {
            LegalMoves = legalMoves;
        }

        public IReadOnlyCollection<ChessMove> LegalMoves { get; private set; }
    }
}
