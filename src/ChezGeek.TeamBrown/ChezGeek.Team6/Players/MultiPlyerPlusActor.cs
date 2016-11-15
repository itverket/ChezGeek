using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Routing;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using ChezGeek.TeamBrown.Actors.MultiPlyer;
using ChezGeek.TeamBrown.Services;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamBrown.Players
{
#if DEBUG
    [ChessPlayer("MultiFembot")]
#endif
    public class MultiPlyerPlusActor : ReceiveActor
    {
        private const int NumberOfWorkerActors = 8;
        private const int NumberOfActorsPerNode = 2;
        private const int DivideRemainingTimeBy = 30;
        private readonly FembotChessCalculationsService _chessCalculationsService;
        private readonly Random _random;
        private IActorRef _workerRouter;
        private int limitMovesOne = 6;
        private int limitMovesTwo = 10;

        public MultiPlyerPlusActor(Player player)
        {
            _random = new Random();
            _chessCalculationsService = new FembotChessCalculationsService();
            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));

            //_workerRouter = Context.ActorOf(Props.Create<MultiPlyerWorkerActor>()
            //    .WithRouter(new ClusterRouterPool(new RoundRobinPool(NumberOfWorkerActors),
            //        new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, false, "node"))));
        }

        protected override void PreStart()
        {
#if DEBUG
            //_workerRouter = Context.ActorOf(Props.Create<MultiPlyerPlusWorkerActor>()
            //    .WithRouter(new RoundRobinPool(4)));
            _workerRouter = Context.ActorOf(Props.Create<MultiPlyerPlusWorkerActor>()
                .WithRouter(new ClusterRouterPool(new RoundRobinPool(4),
                    new ClusterRouterPoolSettings(12, 4, false, "node"))));
#else
            _workerRouter = Context.ActorOf(Props.Create<MultiPlyerPlusWorkerActor>()
                .WithRouter(new ClusterRouterPool(new RoundRobinPool(NumberOfWorkerActors),
                    new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, false, "node"))));
#endif
            base.PreStart();
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var state = getNextMoveQuestion.ChessBoardState;
                var remainingTime = state.NextToMove == Player.Black ? state.BlackTime : state.WhiteTime;
                var limitMovesPly2 = state.NextToMove == Player.Black ? limitMovesOne : limitMovesOne -1;
                var limitMovesPly3 = state.NextToMove == Player.Black ? limitMovesTwo : limitMovesTwo -1;
                var timeLimitSeconds = remainingTime.TotalSeconds / DivideRemainingTimeBy;

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

                if ((state.MoveHistory.Count < limitMovesPly2) || (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds))
                    return ReturnBestMove(legalStateMovesGen1, firstGen2Evaluations);

                // 3 PLY

                var workerQuestions = legalStateMovesGen1ToGen2
                    .Select(y => new MultiPlyerPlusWorkerQuestion(y.Key, y.Value));

                var tasks = workerQuestions.Select(q => _workerRouter.Ask<MultiPlyerPlusWorkerAnswer>(q));
                var answers = await Task.WhenAll(tasks).ConfigureAwait(false);

                ValidateExceptions(answers);

                var secondGen2Evaluations = answers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

                if ((state.MoveHistory.Count < limitMovesPly3) || (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds))
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

            return new KeyValuePair<PreliminaryStateAndMove, MovePlan>(answer.StateAndMove, newPlan);
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