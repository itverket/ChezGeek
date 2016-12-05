using System;
using System.Threading.Tasks;
using Akka.Actor;
using Geek2k16.Entities;
using Geek2k16.Entities.Structs;

namespace Geek2k16.Actors
{
    public class RandomMoveActor : ReceiveActor
    {
        private readonly IActorRef _legalMoves;

        public RandomMoveActor()
        {
            _legalMoves = Context.ActorOf<LegalMovesActor>();

            //Query _legalMoves, and pick one randomly
            ReceiveAsync<GetNextMoveQuestion>(async q => Sender.Tell(await GetRandomMove(q), Self));
        }

        private async Task<GetNextMoveAnswer> GetRandomMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            var result = await _legalMoves.Ask<GetLegalMoveSetAnswer>(new GetLegalMoveSetQuestion(getNextMoveQuestion.ChessBoardState));
            var max = result.LegalMoves.Length;

            if(max == 0)
                return new GetNextMoveAnswer(new ChessMove());

            var selected = new Random().Next(0, max);
            return new GetNextMoveAnswer(result.LegalMoves[selected]);

        }
    }
}