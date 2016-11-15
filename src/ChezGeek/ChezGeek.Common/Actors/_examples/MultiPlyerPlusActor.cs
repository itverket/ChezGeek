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
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace ChezGeek.Common.Actors._examples
{
#if DEBUG
    [ChessPlayer("MultiPlyer+")]
#endif
    public class MultiPlyerPlusActor : ReceiveActor
    {
        private const int NumberOfWorkerActors = 40;
        private const int NumberOfActorsPerNode = 2;
        private const int DivideRemainingTimeBy = 30;
        private readonly CalculationService _calculationsService;
        private readonly Random _random;
        private readonly IActorRef _workerRouter;
        private int _timeLimitSeconds;
        private GetNextMoveAnswer _nextMoveAnswer;

        public MultiPlyerPlusActor(Player player)
        {
            _random = new Random();
            _calculationsService = new CalculationService();
            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));

#if DEBUG
            _workerRouter = Context.ActorOf(Props.Create<MultiPlyerPlusWorkerActor>()
                .WithRouter(new SmallestMailboxPool(NumberOfActorsPerNode)));
#else
            _workerRouter = Context.ActorOf(Props.Create<MultiPlyerWorkerActor>()
                .WithRouter(new ClusterRouterPool(new RoundRobinPool(NumberOfWorkerActors),
                    new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, true, "node"))));
#endif
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            var state = getNextMoveQuestion.ChessBoardState;
            var remainingTime = state.NextToMove == Player.Black ? state.BlackTime : state.WhiteTime;
            var timeQuota = (int) remainingTime.TotalSeconds / DivideRemainingTimeBy;
            _timeLimitSeconds = Math.Min(state.MoveHistory.Count, timeQuota)+3;

            await Task.WhenAny(CalculateBestMove(state), Task.Delay(_timeLimitSeconds*1000));
            await _workerRouter.GracefulStop(new TimeSpan(0, 0, 3));

            return _nextMoveAnswer;
        }

        private async Task CalculateBestMove(ChessBoardState state)
        {
            try
            {
                var rootState = new PreliminaryBoardState(state);
                var availableStateMovesGen1 = _calculationsService.GetCurrentAvailableStateMoves(rootState);

                _nextMoveAnswer = new GetNextMoveAnswer(availableStateMovesGen1.NextMoves.First(), 0);

                // LEGAL MOVES
                
                var availableStateMovesGen1ToGen2 = _calculationsService
                    .GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
                var legalStateMovesGen1 = _calculationsService
                    .FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

                // 2 PLY

                var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                    .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                    .ToDictionary(x => x.Key, x => x.Value);

                var firstGen2Evaluations = legalStateMovesGen1ToGen2.ToDictionary(x => x.Key,
                    x => GetBestPlans(x.Value).GetRandomItem(_random));

                _nextMoveAnswer = ReturnBestMove(legalStateMovesGen1, firstGen2Evaluations);

                // 3 PLY

                var workerQuestions = legalStateMovesGen1ToGen2
                    .Select(y => new MultiPlyerPlusWorkerQuestion(y.Key, y.Value));

                var tasks = workerQuestions.Select(q => _workerRouter.Ask<MultiPlyerPlusWorkerAnswer>(q));
                var answers = await Task.WhenAll(tasks).ConfigureAwait(false);

                ValidateExceptions(answers);

                var secondGen2Evaluations = answers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

                _nextMoveAnswer = ReturnBestMove(legalStateMovesGen1, secondGen2Evaluations);

                // 4 PLY

                var thirdGen2Evaluations = (await Task.WhenAll(answers.Select(AggregateSecondGradeAnswers)))
                    .ToDictionary(x => x.Key, x => x.Value);

                _nextMoveAnswer = ReturnBestMove(legalStateMovesGen1, thirdGen2Evaluations);
            }
            catch (Exception exception)
            {
                _nextMoveAnswer = new GetNextMoveAnswer(exception.Message);
            }
        }
        
        private static MovePlan CreateMovePlan(PreliminaryStateAndMove stateAndMove, MovePlan plan)
        {
            var chainedMoves = new[] { stateAndMove }.Concat(plan.ChainedMoves).ToArray();
            return new MovePlan(chainedMoves, plan.EstimatedValue);
        }

        private MovePlan[] GetBestPlans(AvailableStateMoves thisPlyStateMoves, Dictionary<PreliminaryStateAndMove, MovePlan> thisPlyToNextPlyEvaluations)
        {
            var state = thisPlyStateMoves.State;
            var nextToMove = state.NextToMove;
            var bestPlans = thisPlyStateMoves.NextMoves
                .Select(m => new PreliminaryStateAndMove(state, m))
                .Select(m => CreateMovePlan(m, thisPlyToNextPlyEvaluations[m]))
                .GroupBy(y => y.EstimatedValue * (int)nextToMove)
                .OrderByDescending(x => x.Key)
                .First().ToArray();
            return PreferChecking(bestPlans);
        }

        private MovePlan[] GetBestPlans(AvailableStateMoves leafStateMoves)
        {
            var state = leafStateMoves.State;
            var nextToMove = state.NextToMove;
            var bestPlans = leafStateMoves.NextMoves
                .Select(m => new PreliminaryStateAndMove(state, m))
                .Select(CreateMovePlan)
                .GroupBy(y => y.EstimatedValue * (int)nextToMove)
                .OrderByDescending(x => x.Key)
                .First().ToArray();
            return PreferChecking(bestPlans);
        }

        private MovePlan CreateMovePlan(PreliminaryStateAndMove preliminaryStateAndMove)
        {
            var estimatedValue = EvaluateMove(preliminaryStateAndMove.PreliminaryState.ChessGrid,
                preliminaryStateAndMove.NextMove);
            return new MovePlan(new[] { preliminaryStateAndMove }, estimatedValue);
        }

        private float EvaluateMove(ChessGrid chessGrid, ChessMove move)
        {
            var gridAfterMove = _calculationsService.GetGridAfterMove(chessGrid, move);
            var materialValue = _calculationsService.GetValueOfPieces(gridAfterMove);
            return materialValue;
        }

        private async Task<KeyValuePair<PreliminaryStateAndMove, MovePlan>> AggregateSecondGradeAnswers(MultiPlyerPlusWorkerAnswer answer)
        {
            var newQuestions =
                answer.FromPreviousPlyToThisPly.Select(y => new MultiPlyerPlusWorkerQuestion(y.Key, y.Value, true));
            var newTasks = newQuestions.Select(q => _workerRouter.Ask<MultiPlyerPlusWorkerAnswer>(q));
            var newAnswers = await Task.WhenAll(newTasks).ConfigureAwait(false);

            ValidateExceptions(newAnswers);

            var firstGen3Evaluations = newAnswers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var newPlan = GetBestPlans(answer.AvailableStateMoves, firstGen3Evaluations).GetRandomItem(_random);

            return new KeyValuePair<PreliminaryStateAndMove,MovePlan>(answer.StateAndMove,newPlan);
        }
        
        private MovePlan[] PreferChecking(MovePlan[] movePlans)
        {
            var chekingMovePlans = movePlans.Where(x => IsChecked( x.ChainedMoves.Take(2).Last()) ).ToArray();
            return chekingMovePlans.Any() ? chekingMovePlans : movePlans;
        }

        private bool IsChecked(PreliminaryStateAndMove preliminaryStateAndMove)
        {
            return _calculationsService.IsChecked(preliminaryStateAndMove.PreliminaryState);
        }

        private GetNextMoveAnswer ReturnBestMove(AvailableStateMoves legalStateMovesGen1,
            Dictionary<PreliminaryStateAndMove, MovePlan> firstGen2Evaluations)
        {
            var bestPlan = GetBestPlans(legalStateMovesGen1, firstGen2Evaluations)
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