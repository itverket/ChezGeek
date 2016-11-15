using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using Geek2k16.Entities.Enums;
using Geek2k16.Service;

namespace ChezGeek.Common.Actors._examples
{
#if DEBUG
    [ChessPlayer("Two Plyer")]
#endif
    public class TwoPlyerActor : ReceiveActor
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;

        public TwoPlyerActor(Player player)
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            var state = getNextMoveQuestion.ChessBoardState;

            var availableMoves = _chessCalculationsService.GetAvailableMoves(state);
            var movesWithNextPly = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(state,
                availableMoves);
            var legalMoves = _chessCalculationsService.FilterBySelfMatingMoves(movesWithNextPly, state);
            var legalMovesWithNextPly = movesWithNextPly.Where(x => legalMoves.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            var legalMovesWithNextValues = legalMovesWithNextPly.ToDictionary(x => x.Key, x =>
            {
                var firstMove = x.Key;
                var stateAfterFirstMove = _chessCalculationsService.GetGridAfterMove(state.ChessGrid, firstMove);
                return x.Value.Select(
                        m =>
                                _chessCalculationsService.GetGridAfterMove(stateAfterFirstMove, m))
                    .Select(s => _chessCalculationsService.GetValueOfPieces(s))
                    .OrderBy(y => y*(int) state.NextToMove).First();
            });
            var bestMoves = legalMovesWithNextValues.GroupBy(x => x.Value * (int)state.NextToMove).OrderByDescending(x => x.Key).First().ToArray();
            var chessMove = bestMoves.ElementAt(_random.Next(0, bestMoves.Length));

            return new GetNextMoveAnswer(chessMove.Key, chessMove.Value);
        }
    }
}