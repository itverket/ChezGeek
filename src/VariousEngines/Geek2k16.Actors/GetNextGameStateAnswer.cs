using Geek2k16.Entities;

namespace Geek2k16.Actors
{
    public class GetNextGameStateAnswer
    {
        public GetNextGameStateAnswer(ChessBoardState chessBoardState)
        {
            ChessBoardState = chessBoardState;
        }

        public ChessBoardState ChessBoardState { get; }
    }
}
