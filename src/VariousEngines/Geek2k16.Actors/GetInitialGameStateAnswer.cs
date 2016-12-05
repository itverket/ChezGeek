using Geek2k16.Entities;

namespace Geek2k16.Actors
{
    public class GetInitialGameStateAnswer
    {
        public GetInitialGameStateAnswer(ChessBoardState initialChessBoardState)
        {
            InitialChessBoardState = initialChessBoardState;
        }

        public ChessBoardState InitialChessBoardState { get; set; }
    }
}
