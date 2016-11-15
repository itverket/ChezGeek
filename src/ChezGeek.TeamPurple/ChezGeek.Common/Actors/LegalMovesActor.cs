using Akka.Actor;
using ChezGeek.Common.Messages;
using Geek2k16.Service;

namespace ChezGeek.Common.Actors
{
    public class LegalMovesActor : ReceiveActor
    {
        private readonly ChessCalculationsService _chessCalculationService;

        public LegalMovesActor()
        {
            _chessCalculationService = new ChessCalculationsService();

            Receive<GetLegalMoveSetQuestion>(question => Sender.Tell(GetLegalMoves(question), Self));
        }

        private GetLegalMoveSetAnswer GetLegalMoves(GetLegalMoveSetQuestion getLegalMoveSetQuestion)
        {
            var legalMoves = _chessCalculationService.GetLegalMoves(getLegalMoveSetQuestion.ChessBoardState);

            return new GetLegalMoveSetAnswer(legalMoves);
        }
    }
}
