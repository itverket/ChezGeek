using Geek2k16.Entities;
using Geek2k16.Service;

namespace Geek2k16.Actors
{
    public class CreateNewBoardAnswer : IAnswer
    {
            public readonly ChessBoardState ChessBoardState;

        //ChessSituation
        public CreateNewBoardAnswer(ChessBoardState state)
        {
            ChessBoardState = state;
        }
    }
}