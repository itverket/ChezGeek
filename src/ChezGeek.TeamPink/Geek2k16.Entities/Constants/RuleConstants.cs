using System;
using System.Collections.Generic;
using Geek2k16.Entities.Enums;

namespace Geek2k16.Entities.Constants
{
    public static class RuleConstants
    {
        public static TimeSpan StartTime = new TimeSpan(0, 3, 0);
        public static TimeSpan MoveIncrement = new TimeSpan(0, 0, 2);

        public static Dictionary<StateResult, Player?> WinningPlayer = new Dictionary<StateResult, Player?>
        {
            {StateResult.BlackIsOutOfTime, Player.White},
            {StateResult.BlackKingCheckmated, Player.White},
            {StateResult.BlackIllegalMove, Player.White},
            {StateResult.RepeatStateThreeTimes, null},
            {StateResult.Stalemate, null},
            {StateResult.InsufficientMaterial, null},
            {StateResult.FiftyInconsequentialMoves, null},
            {StateResult.WhiteIsOutOfTime, Player.Black},
            {StateResult.WhiteKingCheckmated, Player.Black},
            {StateResult.WhiteIllegalMove, Player.Black}
        };
    }
}