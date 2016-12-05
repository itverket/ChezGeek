using Akka.Actor;
using Geek2k16.Service;

namespace Geek2k16.Actors
{
    public class LegalMovesActor : ReceiveActor
    {
        private readonly IActorRef _pieceMover;
        private ChessCalculationsService _chessMoveService;

        public LegalMovesActor()
        {
            _pieceMover = Context.ActorOf<PieceMoverActor>();

            _chessMoveService = new ChessCalculationsService();

            // Query _piecemover for each piece of acting color and aggregate into an answer

            Receive<GetLegalMoveSetQuestion>(q => Sender.Tell(GetLegalMoves(q), Self));
        }

        private GetLegalMoveSetAnswer GetLegalMoves(GetLegalMoveSetQuestion q)
        {
            var legalMoves = _chessMoveService.GetLegalMoves(q.ChessBoardState);
            return new GetLegalMoveSetAnswer(legalMoves);
        }
    }
}