using System;
using System.Collections.Generic;
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
    [ChessPlayer("Three Plyer")]
    public class ThreePlyerActor : ReceiveActor
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;

        public ThreePlyerActor(Player player)
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            var state = getNextMoveQuestion.ChessBoardState;

            var rootState = new PreliminaryBoardState(state);
            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = _chessCalculationsService
                .GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService
                .FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

            var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                .ToDictionary(x => x.Key, x => x.Value);

            var availableStateMovesGen2ToGen3 = legalStateMovesGen1ToGen2.Values.AsParallel()
                .Select(g2 => _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(g2))
                .ToArray();

            var gen2ToGen3Evaluations = availableStateMovesGen2ToGen3.AsParallel()
                .Select(x => x.ToDictionary(g2 => g2.Key, g2 => GetRandomItem(_chessCalculationsService.GetBestPlans(g2.Value))))
                .SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);

            var gen1ToGen2Evaluations = legalStateMovesGen1ToGen2
                .ToDictionary(g1 => g1.Key, g1 => GetRandomItem(_chessCalculationsService.GetBestPlans(g1.Value, gen2ToGen3Evaluations)));

            var bestPlan = GetRandomItem(_chessCalculationsService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations));
            var bestMove = bestPlan.ChainedMoves.First();

            return new GetNextMoveAnswer(bestMove.NextMove, bestPlan.EstimatedValue);
        }

        private TValue GetRandomItem<TValue>(ICollection<TValue> values)
        {
            return values.ElementAt(_random.Next(0, values.Count));
        }
    }
}