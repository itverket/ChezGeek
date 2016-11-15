using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using Geek2k16.Entities.Enums;

namespace ChezGeek.Common.Actors._examples
{
    [ChessPlayer("Randomizer2000")]
    public class RandomMoveActor : ReceiveActor
    {
        private readonly IActorRef _legalMovesActor;
        private readonly Random _random;

        public RandomMoveActor(Player player)
        {
            _random = new Random();
            _legalMovesActor = Context.ActorOf<LegalMovesActor>();

            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetRandomMove(question), Self));
        }

        private async Task<GetNextMoveAnswer> GetRandomMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            var result = await _legalMovesActor.Ask<GetLegalMoveSetAnswer>(
                new GetLegalMoveSetQuestion(getNextMoveQuestion.ChessBoardState));

            var legalMoveCount = result.LegalMoves.Count;

            if (legalMoveCount == 0)
                throw new InvalidOperationException("No legal moves");

            var randomMoveIndex = _random.Next(0, legalMoveCount);

            var strength = randomMoveIndex;

            return new GetNextMoveAnswer(result.LegalMoves.ElementAt(randomMoveIndex), strength);
        }
    }
}
