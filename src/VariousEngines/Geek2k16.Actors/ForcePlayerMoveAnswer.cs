using Geek2k16.Entities;

namespace Geek2k16.Actors
{
    public class ForcePlayerMoveAnswer : IAnswer
    {
        public ChessBoardState ChessBoardState { get; set; }
    }
}