using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace ChezGeek.Common.Actors._examples
{
    [ChessPlayer("One Plyer")]
    public class OnePlyerActor : ReceiveActor
    {
        private readonly IActorRef _legalMovesActor;
        private readonly ChessCalculationsService _chessCalculationService;
        private readonly Random _random;

        public OnePlyerActor(Player player)
        {
            _random = new Random();
            _chessCalculationService = new ChessCalculationsService();
            _legalMovesActor = Context.ActorOf<LegalMovesActor>();

            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            var state = getNextMoveQuestion.ChessBoardState;
            var result = await _legalMovesActor.Ask<GetLegalMoveSetAnswer>(
                new GetLegalMoveSetQuestion(state));
            var legalMoves = result.LegalMoves;

            if (legalMoves.Count == 0)
                throw new InvalidOperationException("No legal moves");
            
            var bestMoves = legalMoves
                .Select(x => new {move = x, state = _chessCalculationService.GetStateAfterMove(state, new ExecutedChessMove(x, TimeSpan.Zero))})
                .Select(x => new {x.move, value = _chessCalculationService.GetValueOfPieces(x.state)})
                .GroupBy(x => x.value * (int)state.NextToMove).OrderByDescending(x => x.Key).First().ToArray();
            var chessMove = bestMoves.ElementAt(_random.Next(0, bestMoves.Length));
            return new GetNextMoveAnswer(chessMove.move, chessMove.value);
        }
    }
}