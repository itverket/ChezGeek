using System;

namespace Geek2k16.Entities.Enums
{
    [Flags]
    public enum MoveOption
    {
        ConvertPawnToQueen = 0x08,
        ConvertPawnToKnight = 0x10
    }
}