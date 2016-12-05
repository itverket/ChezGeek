using Akka.Actor;
using Geek2k16.Service;

namespace Geek2k16.Actors
{
    public class SetupActor : ReceiveActor
    {


        public SetupActor()
        {
            // Return default setup chess board, with White/Black players tagged in ChessSituation
            Receive<CreateNewBoardQuestion>(q => Sender.Tell(CreateNewBoard(), Self));
        }

        private static CreateNewBoardAnswer CreateNewBoard()
        {
            var chessStateService = new ChessCalculationsService();
            var state = chessStateService.GetInitialState();
            return new CreateNewBoardAnswer(state);
        }
    }
}