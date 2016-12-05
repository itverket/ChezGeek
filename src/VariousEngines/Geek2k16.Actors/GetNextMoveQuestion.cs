using Geek2k16.Entities;

namespace Geek2k16.Actors
{
    public class GetNextMoveQuestion : IQuestion
    {
        //ChessSituation
        public ChessBoardState ChessBoardState { get; set; }
    }
}