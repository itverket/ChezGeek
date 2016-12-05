using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace Geek2k16.Actors
{
    public class ArbiterActor : ReceiveActor
    {
        private readonly IActorRef _setup;
        private readonly IActorRef _legalMoves;

        public ArbiterActor()
        {
            _setup = Context.ActorOf<SetupActor>();
            _legalMoves = Context.ActorOf<LegalMovesActor>();

            ReceiveAsync<CreateNewBoardQuestion>(async q => Sender.Tell(await QueryForSetupPosition(q), Self));
            ReceiveAsync<GetLegalMoveSetQuestion>(async q => Sender.Tell(await QueryForLegalMoves(q), Self));

            ReceiveAsync<IsLegalMoveQuestion>(async q => Sender.Tell(await IsLegalMove(q), Self));

            // Handle unhandled Questions and Messages
            Receive<IQuestion>(q => Sender.Tell("ERROR", Self));
            Receive<IMessage>(q => Sender.Tell("ERROR", Self));
        }

        private async Task<IsLegalMoveAnswer> IsLegalMove(IsLegalMoveQuestion q)
        {
            var legalMovesAnswer = await QueryForLegalMoves(new GetLegalMoveSetQuestion(q.ChessBoardState));
            var isLegal = legalMovesAnswer.LegalMoves.ToList().IndexOf(q.ChessMove) > -1;
            return new IsLegalMoveAnswer(isLegal);

        }

        private async Task<GetLegalMoveSetAnswer> QueryForLegalMoves(GetLegalMoveSetQuestion q)
        {
            return await _legalMoves.Ask<GetLegalMoveSetAnswer>(q);
        }

        private async Task<CreateNewBoardAnswer> QueryForSetupPosition(CreateNewBoardQuestion q)
        {
            return await _setup.Ask<CreateNewBoardAnswer>(q);
        }


    }
}