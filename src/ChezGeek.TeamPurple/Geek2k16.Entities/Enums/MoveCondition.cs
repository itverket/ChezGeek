using System;

namespace Geek2k16.Entities.Enums
{
    [Flags]
    public enum MoveCondition
    {
        MoveThroughPieces = 0x01,
        MustCapture = 0x02,
        CannotCapture = 0x04,
        UnlimitedDistance = 0x08,
        DoubleMoveIfStartRow = 0x10,
        CastleShort = 0x20,
        CastleLong = 0x40
    }
}