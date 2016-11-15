using Geek2k16.Entities.Enums;
using System;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct PositionOffset
    {
        public PositionOffset(int columns, int rows, MoveCondition moveCondition = 0)
        {
            Columns = columns;
            Rows = rows;
            MoveCondition = moveCondition;
        }

        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public MoveCondition MoveCondition { get; private set; }

        public PositionOffset AsPlayer(Player player)
        {
            return new PositionOffset(Columns, Rows*(int)player, MoveCondition);
        }
    }
}