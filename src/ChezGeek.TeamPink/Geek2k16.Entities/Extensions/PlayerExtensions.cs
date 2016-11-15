using Geek2k16.Entities.Enums;

namespace Geek2k16.Common.Extensions
{
    public static class PlayerExtensions
    {
        public static Player Reverse(this Player player)
        {
            return player == Player.Black ? Player.White : Player.Black;
        }
    }
}