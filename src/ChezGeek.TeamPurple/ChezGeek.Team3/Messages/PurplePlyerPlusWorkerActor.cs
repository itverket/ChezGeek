using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Actors._examples;
using Geek2k16.Service;

namespace ChezGeek.TeamPurple.Messages
{
    public class PurplePlyerPlusWorkerActor : ReceiveActor
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;

        public PurplePlyerPlusWorkerActor()
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<TeamPurple.Messages.PurplePlyerPlusWorkerQuestion>(async question => Sender.Tell(await GetBestMoves(question), Self));
        }

        private async Task<TeamPurple.Messages.PurplePlyerPlusWorkerAnswer> GetBestMoves(TeamPurple.Messages.PurplePlyerPlusWorkerQuestion question)
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
                return new TeamPurple.Messages.PurplePlyerPlusWorkerAnswer(question, bestPlan, fromPreviousPlyToThisPly);
            }
            catch (Exception exception)
            {
                return new TeamPurple.Messages.PurplePlyerPlusWorkerAnswer(exception.Message);
            }
        }
    }
}