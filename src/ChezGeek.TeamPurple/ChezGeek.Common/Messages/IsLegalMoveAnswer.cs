using ChezGeek.Common.Serialization;
using System;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class IsLegalMoveAnswer : SerializableMessage
    {
        public IsLegalMoveAnswer(bool isLegal)
        {
            IsLegal = isLegal;
        }

        public bool IsLegal { get; private set; }
    }
}
