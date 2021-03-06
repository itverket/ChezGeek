﻿using ChezGeek.Common.Serialization;
using Geek2k16.Entities;
using System;

namespace ChezGeek.Common.Messages
{
    [Serializable]
    public class GetLegalMoveSetQuestion : SerializableMessage
    {
        public GetLegalMoveSetQuestion(ChessBoardState chessBoardState)
        {
            ChessBoardState = chessBoardState;
        }

        public ChessBoardState ChessBoardState { get; private set; }
    }
}
