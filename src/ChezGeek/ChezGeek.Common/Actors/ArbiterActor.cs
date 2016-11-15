using Akka.Actor;
using ChezGeek.Common.Messages;
using System.Threading.Tasks;
using System.Linq;
using Geek2k16.Service;

namespace ChezGeek.Common.Actors
{
    public class ArbiterActor : ReceiveActor
    {
        private readonly IActorRef _legalMovesActor;
        private readonly ChessCalculationsService _cessCalculationService;

        public ArbiterActor()
        {
            _legalMovesActor = Context.ActorOf<LegalMovesActor>();
            _cessCalculationService = new ChessCalculationsService();

            Receive<GetInitialBoardStateQuestion>(_ => Sender.Tell(GetInitialBoardState(), Self));

            Receive<GetNextBoardStateQuestion>(
                getNextBoardStateQuestion => Sender.Tell(GetNextBoardState(getNextBoardStateQuestion), Self));

            ReceiveAsync<IsLegalMoveQuestion>(
                async isLegalMoveQuestion => Sender.Tell(await IsLegalMove(isLegalMoveQuestion), Self));
        }

        private GetInitialBoardStateAnswer GetInitialBoardState()
        {
            return new GetInitialBoardStateAnswer(_cessCalculationService.GetInitialState());
        }

        private GetNextBoardStateAnswer GetNextBoardState(GetNextBoardStateQuestion getNextBoardStateQuestion)
        {
            var nextBoardState = _cessCalculationService.GetStateAfterMove(
                getNextBoardStateQuestion.ChessBoardState, getNextBoardStateQuestion.ExecutedChessMove);

            return new GetNextBoardStateAnswer(nextBoardState);
        }

        private async Task<IsLegalMoveAnswer> IsLegalMove(IsLegalMoveQuestion isLegalMoveQuestion)
        {
            var legalMovesAnswer = await _legalMovesActor.Ask<GetLegalMoveSetAnswer>(
                new GetLegalMoveSetQuestion(isLegalMoveQuestion.ChessBoardState));

            var isLegal = legalMovesAnswer.LegalMoves.Contains(isLegalMoveQuestion.ChessMove);

            return new IsLegalMoveAnswer(isLegal);
        }
    }
}
