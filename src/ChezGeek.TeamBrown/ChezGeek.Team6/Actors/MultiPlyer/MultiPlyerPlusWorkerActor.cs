using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.TeamBrown.Services;
using Geek2k16.Service;

namespace ChezGeek.TeamBrown.Actors.MultiPlyer
{
    public class MultiPlyerPlusWorkerActor : ReceiveActor
    {
        private readonly FembotChessCalculationsService2 _chessCalculationsService;
        private readonly Random _random;

        public MultiPlyerPlusWorkerActor()
        {
            _random = new Random();
            _chessCalculationsService = new FembotChessCalculationsService2();
            ReceiveAsync<MultiPlyerPlusWorkerQuestion>(async question => Sender.Tell(await GetBestMoves(question), Self));
            ReceiveAny(_ =>
            {
                Console.WriteLine("Any");
            });
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
                        .GetBestPlans(previousPly, thisPlyEvaluations).GetRandomItem(_random);
                return new MultiPlyerPlusWorkerAnswer(question, bestPlan, fromPreviousPlyToThisPly);
            }
            catch (Exception exception)
            {
                return new MultiPlyerPlusWorkerAnswer(exception.Message);
            }
        }
    }
}