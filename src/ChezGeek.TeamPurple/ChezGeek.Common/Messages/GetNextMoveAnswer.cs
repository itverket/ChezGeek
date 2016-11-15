using System;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class GetNextMoveAnswer : SerializableMessage
    {
        public GetNextMoveAnswer(string exceptionMessage)
        {
            ExceptionMessage = exceptionMessage;
        }

        public GetNextMoveAnswer(ChessMove chessMove, float perceivedStrength)
        {
            ChessMove = chessMove;
            PerceivedStrength = perceivedStrength;
        }

        public ChessMove ChessMove { get; private set; }
        public float PerceivedStrength { get; private set; }
        public string ExceptionMessage { get; private set; }
    }
}