using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace ChezGeek.Common.Actors._examples
{
    [ChessPlayer("MultiPlyer+")]
    public class MultiPlyerPlusActor : ReceiveActor
    {
        private const int NumberOfWorkerActors = 8;
        private const int NumberOfActorsPerNode = 2;
        private const int DivideRemainingTimeBy = 30;
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;
        private readonly IActorRef _workerRouter;

        public MultiPlyerPlusActor(Player player)
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));
            _workerRouter = Context.ActorOf(Props.Create<MultiPlyerPlusWorkerActor>()
                .WithRouter(new RoundRobinPool(NumberOfActorsPerNode)));
            //_workerRouter = Context.ActorOf(Props.Create<MultiPlyerWorkerActor>()
            //    .WithRouter(new ClusterRouterPool(new RoundRobinPool(NumberOfWorkerActors),
            //        new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, true, "node"))));
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var state = getNextMoveQuestion.ChessBoardState;
                var remainingTime = state.NextToMove == Player.Black ? state.BlackTime : state.WhiteTime;
                var timeLimitSeconds = remainingTime.TotalSeconds/DivideRemainingTimeBy;

                var rootState = new PreliminaryBoardState(state);

                // LEGAL MOVES

                var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
                var availableStateMovesGen1ToGen2 = _chessCalculationsService
                    .GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
                var legalStateMovesGen1 = _chessCalculationsService
                    .FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

                // 2 PLY

                var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                    .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                    .ToDictionary(x => x.Key, x => x.Value);

                var firstGen2Evaluations = legalStateMovesGen1ToGen2.ToDictionary(x => x.Key,
                    x => _chessCalculationsService.GetBestPlans(x.Value).GetRandomItem(_random));

                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds)
                    return ReturnBestMove(legalStateMovesGen1, firstGen2Evaluations);

                // 3 PLY

                var workerQuestions = legalStateMovesGen1ToGen2
                    .Select(y => new MultiPlyerPlusWorkerQuestion(y.Key, y.Value));

                var tasks = workerQuestions.Select(q => _workerRouter.Ask<MultiPlyerPlusWorkerAnswer>(q));
                var answers = await Task.WhenAll(tasks).ConfigureAwait(false);

                ValidateExceptions(answers);

                var secondGen2Evaluations = answers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds)
                    return ReturnBestMove(legalStateMovesGen1, secondGen2Evaluations);

                // 4 PLY

                var thirdGen2Evaluations = (await Task.WhenAll(answers.Select(AggregateSecondGradeAnswers)))
                    .ToDictionary(x => x.Key, x => x.Value);

                return ReturnBestMove(legalStateMovesGen1, thirdGen2Evaluations);
            }
            catch (Exception exception)
            {
                return new GetNextMoveAnswer(exception.Message);
            }
        }

        private async Task<KeyValuePair<PreliminaryStateAndMove, MovePlan>> AggregateSecondGradeAnswers(MultiPlyerPlusWorkerAnswer answer)
        {
            var newQuestions =
                answer.FromPreviousPlyToThisPly.Select(y => new MultiPlyerPlusWorkerQuestion(y.Key, y.Value, true));
            var newTasks = newQuestions.Select(q => _workerRouter.Ask<MultiPlyerPlusWorkerAnswer>(q));
            var newAnswers = await Task.WhenAll(newTasks).ConfigureAwait(false);

            ValidateExceptions(newAnswers);

            var firstGen3Evaluations = newAnswers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var newPlan = _chessCalculationsService
                .GetBestPlans(answer.AvailableStateMoves, firstGen3Evaluations)
                .GetRandomItem(_random);

            return new KeyValuePair<PreliminaryStateAndMove,MovePlan>(answer.StateAndMove,newPlan);
        }

        private GetNextMoveAnswer ReturnBestMove(AvailableStateMoves legalStateMovesGen1,
            Dictionary<PreliminaryStateAndMove, MovePlan> firstGen2Evaluations)
        {
            var bestPlan = _chessCalculationsService
                .GetBestPlans(legalStateMovesGen1, firstGen2Evaluations)
                .GetRandomItem(_random);
            var bestMove = bestPlan.ChainedMoves.First();
            return new GetNextMoveAnswer(bestMove.NextMove, bestPlan.EstimatedValue);
        }

        private static void ValidateExceptions(MultiPlyerPlusWorkerAnswer[] answers)
        {
            var exception = answers.FirstOrDefault(x => x.ExceptionMessage != null)?.ExceptionMessage;
            if (exception != null) throw new Exception(exception);
        }
    }
}