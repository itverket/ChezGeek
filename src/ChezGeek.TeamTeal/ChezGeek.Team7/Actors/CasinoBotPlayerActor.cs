using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamTeal.Actors
{
    [ChessPlayer("casino.bot")]
    public class CasinosActor : ReceiveActor
    {
        private const int NumberOfWorkerActors = 30;
        private const int NumberOfActorsPerNode = 4;
        private const int DivideRemainingTimeBy = 30;
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly IActorRef _workerRouter;
        private Random _random = new Random();

        public CasinosActor(Player player)
        {
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));
            _workerRouter = Context.ActorOf(Props.Create<CasinoWorkerActor>()
                .WithRouter(new RoundRobinPool(NumberOfActorsPerNode)));
            //_workerRouter = Context.ActorOf(Props.Create<CasinoWorkerActor>()
            //    .WithRouter(new ClusterRouterPool(new RoundRobinPool(NumberOfWorkerActors),
            //        new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, false, "node"))));
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var state = getNextMoveQuestion.ChessBoardState;
                var remainingTime = state.NextToMove == Player.Black ? state.BlackTime : state.WhiteTime;
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
                    x => _chessCalculationsService.GetBestPlans(x.Value, state.MoveHistory.Count).GetRandomItem(_random));

                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds || state.MoveHistory.Count <= 10)
                    return ReturnBestMove(legalStateMovesGen1, firstGen2Evaluations, state.MoveHistory.Count);

                // 3 PLY

                var workerQuestions = legalStateMovesGen1ToGen2
                    .Select(y => new CasinoWorkerQuestion(y.Key, y.Value, state.MoveHistory.Count));

                var tasks = workerQuestions.Select(q => _workerRouter.Ask<CasinoWorkerAnswer>(q));
                var answers = await Task.WhenAll(tasks).ConfigureAwait(false);

                ValidateExceptions(answers);

                var secondGen2Evaluations = answers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds)
                    return ReturnBestMove(legalStateMovesGen1, secondGen2Evaluations, state.MoveHistory.Count);

                // 4 PLY

                var thirdGen2Evaluations = (await Task.WhenAll(answers.Select(AggregateSecondGradeAnswers)))
                    .ToDictionary(x => x.Key, x => x.Value);

                return ReturnBestMove(legalStateMovesGen1, thirdGen2Evaluations, state.MoveHistory.Count);
            }
            catch (Exception exception)
            {
                return new GetNextMoveAnswer(exception.Message);
            }
        }

        private async Task<KeyValuePair<PreliminaryStateAndMove, MovePlan>> AggregateSecondGradeAnswers(CasinoWorkerAnswer answer, int numberOfMoves)
        {
            var newQuestions =
                answer.FromPreviousPlyToThisPly.Select(y => new CasinoWorkerQuestion(y.Key, y.Value, numberOfMoves, true));
            var newTasks = newQuestions.Select(q => _workerRouter.Ask<CasinoWorkerAnswer>(q));
            var newAnswers = await Task.WhenAll(newTasks).ConfigureAwait(false);

            ValidateExceptions(newAnswers);

            var firstGen3Evaluations = newAnswers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var newPlan = _chessCalculationsService
                .GetBestPlans(answer.AvailableStateMoves, firstGen3Evaluations, numberOfMoves)
                .GetRandomItem(_random);

            return new KeyValuePair<PreliminaryStateAndMove, MovePlan>(answer.StateAndMove, newPlan);
        }

        private GetNextMoveAnswer ReturnBestMove(AvailableStateMoves legalStateMovesGen1,
            Dictionary<PreliminaryStateAndMove, MovePlan> firstGen2Evaluations, int numberOfMoves)
        {
            var bestPlans = _chessCalculationsService
                .GetBestPlans(legalStateMovesGen1, firstGen2Evaluations, numberOfMoves);
            var bestPlan = PhasesEvaluator.Boost(bestPlans, numberOfMoves).First();
            var bestMove = bestPlan.ChainedMoves.First();
            return new GetNextMoveAnswer(bestMove.NextMove, bestPlan.EstimatedValue);
        }



        private static void ValidateExceptions(CasinoWorkerAnswer[] answers)
        {
            var exception = answers.FirstOrDefault(x => x.ExceptionMessage != null)?.ExceptionMessage;
            if (exception != null) throw new Exception(exception);
        }
    }
}