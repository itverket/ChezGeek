using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Geek2k16.Service;

namespace ChezGeek.Common.Actors._examples
{
    public class MultiPlyerPlusWorkerActor : ReceiveActor
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;

        public MultiPlyerPlusWorkerActor()
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<MultiPlyerPlusWorkerQuestion>(async question => Sender.Tell(await GetBestMoves(question), Self));
        }

        private async Task<MultiPlyerPlusWorkerAnswer> GetBestMoves(MultiPlyerPlusWorkerQuestion question)
        {
            try
            {
                var previousPly = question.AvailableStateMoves;
                var fromPreviousPlyToThisPly = _chessCalculationsService
                    .GetAvailableMovesWithNextPlyOfAvailableMoves(previousPly);
                var thisPlyEvaluations = fromPreviousPlyToThisPly.ToDictionary(x => x.Key,
                    x => _chessCalculationsService.GetBestPlans(x.Value).GetRandomItem(_random));
                var bestPlan = _chessCalculationsService
                    .GetBestPlans(previousPly, thisPlyEvaluations)
                    .GetRandomItem(_random);
                return new MultiPlyerPlusWorkerAnswer(question, bestPlan, fromPreviousPlyToThisPly);
            }
            catch (Exception exception)
            {
                return new MultiPlyerPlusWorkerAnswer(exception.Message);
            }
        }
    }
}