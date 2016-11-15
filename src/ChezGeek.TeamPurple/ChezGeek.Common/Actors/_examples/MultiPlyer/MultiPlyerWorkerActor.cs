using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Geek2k16.Service;

namespace ChezGeek.Common.Actors._examples
{
    public class MultiPlyerWorkerActor : ReceiveActor
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;

        public MultiPlyerWorkerActor()
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<MultiPlyerWorkerQuestion>(async question => Sender.Tell(await GetBestMoves(question), Self));
        }

        protected override void PreStart()
        {
            Console.WriteLine($"Worker {Self.Path.Name} is ready to rumble!");

            base.PreStart();
        }

        protected override void PostStop()
        {
            Console.WriteLine($"{Self.Path.Name}: Goodbye, my lover. Goodbye, my fried. You have been the one.... You have been the one for me!");

            base.PostStop();
        }

        private async Task<MultiPlyerWorkerAnswer> GetBestMoves(MultiPlyerWorkerQuestion question)
        {
            var stateMoves = question.AvailableStateMoves;

            var availableStateMovesGen2ToGen3 = _chessCalculationsService
                .GetAvailableMovesWithNextPlyOfAvailableMoves(stateMoves);

            var gen2ToGen3Evaluations = availableStateMovesGen2ToGen3
                .ToDictionary(x => x.Key, x => _chessCalculationsService.GetBestPlans(x.Value).GetRandomItem(_random));

            var bestPlan = _chessCalculationsService.GetBestPlans(stateMoves, gen2ToGen3Evaluations).GetRandomItem(_random);

            Console.WriteLine($"Calculated move: {question.StateAndMove.NextMove.FromPosition} -> {question.StateAndMove.NextMove.ToPosition} (Score: {bestPlan.EstimatedValue})");
            
            return new MultiPlyerWorkerAnswer(question.StateAndMove, bestPlan);
        }
    }
}