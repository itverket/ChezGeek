using ChezGeek.Common.Actors;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;

namespace ChezGeek.UI
{
    public static class PlayerTimeHelper
    {
        public static string SetPlayerTimeRemaining(ChessBoardStateViewModel state, Player player)
        {
            var timeFormat = @"hh\.mm\:ss";
            return player == Player.White ? state.WhiteTime.ToString(timeFormat) : state.BlackTime.ToString(timeFormat);
        }

    }
}