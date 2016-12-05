using Geek2k16.Entities;

namespace Geek2k16.Actors
{
    public class GetBoardStateAnswer: IAnswer
    {
        public ChessBoardState State { get; set; }
    }
}