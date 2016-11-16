using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Serialization;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamTeal.Actors.CasinoBot
{
    public class CasinoWorkerActor : ReceiveActor
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private Random _random;

        public CasinoWorkerActor()
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<CasinoWorkerQuestion>(async question => Sender.Tell(await GetBestMoves(question), Self));
        }

        private async Task<CasinoWorkerAnswer> GetBestMoves(CasinoWorkerQuestion question)
        {
            try
            {
                var previousPly = question.AvailableStateMoves;
                var fromPreviousPlyToThisPly = _chessCalculationsService
                    .GetAvailableMovesWithNextPlyOfAvailableMoves(previousPly);
                var thisPlyEvaluations = fromPreviousPlyToThisPly.ToDictionary(x => x.Key,
                    x => _chessCalculationsService.GetBestPlans(x.Value, question.NumberOfMoves).GetRandomItem(_random));
                var bestPlans = _chessCalculationsService
                    .GetBestPlans(previousPly, thisPlyEvaluations, question.NumberOfMoves);

                var bestPlan = PhasesEvaluator.Boost(bestPlans, question.NumberOfMoves).First();
                return new CasinoWorkerAnswer(question, bestPlan, fromPreviousPlyToThisPly);
            }
            catch (Exception exception)
            {
                return new CasinoWorkerAnswer(exception.Message);
            }
        }
    }

    [Serializable]
    public class CasinoWorkerAnswer : SerializableMessage
    {
        public CasinoWorkerAnswer(CasinoWorkerQuestion question, MovePlan bestPlan,
            Dictionary<PreliminaryStateAndMove, AvailableStateMoves> fromPreviousPlyToThisPly)
        {
            StateAndMove = question.StateAndMove;
            AvailableStateMoves = question.IsLeafNode ? new AvailableStateMoves() : question.AvailableStateMoves;
            MovePlan = bestPlan;
            FromPreviousPlyToThisPly = question.IsLeafNode ? null : fromPreviousPlyToThisPly;
        }

        public CasinoWorkerAnswer(string exceptionMessage)
        {
            ExceptionMessage = exceptionMessage;
        }

        public CasinoWorkerQuestion Question { get; set; }

        public Dictionary<PreliminaryStateAndMove, AvailableStateMoves> FromPreviousPlyToThisPly { get; set; }

        public MovePlan MovePlan { get; set; }

        public AvailableStateMoves AvailableStateMoves { get; set; }

        public PreliminaryStateAndMove StateAndMove { get; set; }
        public string ExceptionMessage { get; set; }
    }

    [Serializable]
    public class CasinoWorkerQuestion : SerializableMessage
    {
        public CasinoWorkerQuestion(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves,
            int numberOfMoves, bool isLeafNode = false)
        {
            StateAndMove = stateAndMove;
            AvailableStateMoves = availableStateMoves;
            NumberOfMoves = numberOfMoves;
            IsLeafNode = isLeafNode;
        }

        public AvailableStateMoves AvailableStateMoves { get; set; }
        public int NumberOfMoves { get; set; }
        public PreliminaryStateAndMove StateAndMove { get; set; }
        public bool IsLeafNode { get; set; }
    }
}