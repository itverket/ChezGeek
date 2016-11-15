using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Routing;
using Akka.Util.Internal;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using ChezGeek.TeamPurple.Messages;
using ChezGeek.TeamPurple.Openings;
using ChezGeek.TeamPurple.Services;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace ChezGeek.TeamPurple.Actors
{
    //[ChessPlayer("Deep Purple")]
    public class TeamPurpleActorBackup : ReceiveActor, IWithUnboundedStash
    {
        private const int NumberOfWorkerActors = 40;
        private const int NumberOfActorsPerNode = 8;
        private const int DivideRemainingTimeBy = 20;

        private readonly Player _player;
        private readonly ChessCalculationsService _chessCalculationsService;
        private CancellationTokenSource _cancellationTokenSource;
        private PurpleCalculationsService _purpleCalculationsService;
        private readonly Random _random;
        private readonly SpanishOpening _spanishOpening;
        public IStash Stash { get; set; }

        private IActorRef _evaluationRouter;
        private readonly IActorRef _workerRouter;

        private Stopwatch stopwatch;
        private double timeLimitSeconds;

        private EvaluatedChessMove _currentBestMove;

        public TeamPurpleActorBackup(Player player)
        {
            _player = player;
            _cancellationTokenSource = new CancellationTokenSource();
            _chessCalculationsService = new ChessCalculationsService();
            _purpleCalculationsService = new PurpleCalculationsService(_chessCalculationsService);
            _random = new Random();

            //_workerRouter = Context.ActorOf(Props.Create<PurplePlyerPlusWorkerActor>()
            //   .WithRouter(new RoundRobinPool(NumberOfActorsPerNode)));

            _spanishOpening = new SpanishOpening();

            Idle();
        }
        protected override void PreStart()
        {
            _evaluationRouter = Context.ActorOf(Props.Create<ChessMoveEvaluatorActor>()
                .WithRouter(new ClusterRouterPool(new SmallestMailboxPool(NumberOfWorkerActors),
                new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, true, "node"))));

            base.PreStart();
        }
        private void Idle()
        {
            Receive<GetNextMoveQuestion>(question =>
            {
                Become(Active);
                StartGetNextMove2(question.ChessBoardState, Sender);
            });
            //ReceiveAsync<GetNextMoveQuestion>(async question =>
            //{
            //    Become(Active);
            //    await Something(question);
            //});
        }

        private async Task Something(GetNextMoveQuestion question)
        {
            stopwatch = Stopwatch.StartNew();
            var state = question.ChessBoardState;
            var remainingTime = state.NextToMove == Player.Black ? state.BlackTime : state.WhiteTime;
            timeLimitSeconds = remainingTime.TotalSeconds / DivideRemainingTimeBy;


            //var timer = Task.Delay(TimeSpan.FromSeconds(timeLimitSeconds - stopwatch.Elapsed.TotalSeconds));
            //var timer = Task.Delay(TimeSpan.FromSeconds(5));
            //await Task.WhenAny(StartGetNextMove(question.ChessBoardState, Sender), timer);
        }
        private void BecomeIdle()
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            Stash.UnstashAll(); Become(Idle);
        }
        private void Active()
        {
            Receive<MoveSelected>(moveSelected =>
            {
                moveSelected.Sender.Tell(new GetNextMoveAnswer(moveSelected.ChosenChessMove, moveSelected.MoveScore), Self);
                BecomeIdle();
            });

            Receive<MoveSelectionCancelled>(_ => BecomeIdle());

            Receive<Cancel>(_ => _cancellationTokenSource.Cancel());

            ReceiveAny(_ => Stash.Stash());
        }

        private async Task StartGetNextMove(ChessBoardState state, IActorRef sender)
        {
            await Task.Run(() => GetNextMove(state, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token)
                .ContinueWith<object>(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                        return new MoveSelectionCancelled();

                    return new MoveSelected(sender, task.Result.Move, task.Result.Score);
                }).PipeTo(Self);
        }

        private void StartGetNextMove2(ChessBoardState state, IActorRef sender)
        {
            Task.Run(() => GetNextMove(state, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token)
                .ContinueWith<object>(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                        return new MoveSelectionCancelled();

                    return new MoveSelected(sender, _currentBestMove.Move, _currentBestMove.Score);
                }).PipeTo(Self);
        }

        private async Task<EvaluatedChessMove> GetNextMove(ChessBoardState state, CancellationToken cancellationToken)
        {
            var rootState = new PreliminaryBoardState(state);

            // LEGAL MOVES
            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService.FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

            //var openingMove = _spanishOpening.GetNextMove(state);

            //if (openingMove.MoveDistance > 0)
            //{
            //    if (legalStateMovesGen1.NextMoves.Contains(openingMove))
            //    {
            //        return new EvaluatedChessMove
            //        {
            //            Move = openingMove,
            //            Score = _chessCalculationsService.GetValueOfPieces(state)
            //        };
            //    }
            //}

            if (legalStateMovesGen1.NextMoves.Count == 1)
            {
                var onlyMove = legalStateMovesGen1.NextMoves.Single();
                return new EvaluatedChessMove
                {
                    Move = onlyMove,
                    Score = _chessCalculationsService.GetValueOfPieces(state)
                };
            }

            // 2 PLY
            var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                .ToDictionary(x => x.Key, x => x.Value);

            var evaluationMessages = legalStateMovesGen1ToGen2
                .Select(y => new EvaluateMove(y.Key, y.Value));

            var evaluationTasks = evaluationMessages
                .Select(message => _evaluationRouter.Ask<MoveEvaluated>(message));

            var evaluatedMoves = await Task.WhenAll(evaluationTasks)
                .ConfigureAwait(false);


            // 3 PLY +
            //if (stopwatch.Elapsed.TotalSeconds < timeLimitSeconds)
            //{
            //    var timeSpan = TimeSpan.FromSeconds(timeLimitSeconds - stopwatch.Elapsed.TotalSeconds);
            //    var availableStateMoves = evaluatedMoves.Select(e => _chessCalculationsService.GetCurrentAvailableStateMoves(e.StateAndMove.PreliminaryState));
            //    var availableStateMovesNextGen = availableStateMoves.Select(a => _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(a));
            //    evaluationMessages = availableStateMovesNextGen.SelectMany(a => a.Select(m => new EvaluateMove(m.Key, m.Value)));
            //    var num = evaluationMessages.Count();
            //    evaluationTasks = evaluationMessages
            //        .Select(message => _evaluationRouter.Ask<MoveEvaluated>(message));

            //    evaluatedMoves = await Task.WhenAll(evaluationTasks).ConfigureAwait(false);
            //    SetCurrentBestMove(evaluatedMoves, legalStateMovesGen1);
            //}
            var gen1ToGen2Evaluations = evaluatedMoves
                .ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var bestPlan =
                GetRandomItem(_purpleCalculationsService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations));

            var bestMove =
                bestPlan.ChainedMoves.First();

            return new EvaluatedChessMove { Move = bestMove.NextMove, Score = bestPlan.EstimatedValue };
        }

        private void SetCurrentBestMove(MoveEvaluated[] evaluatedMoves, AvailableStateMoves legalStateMoves)
        {
            // EVALUATE
            var gen1ToGen2Evaluations = evaluatedMoves
                .ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var bestPlan =
                GetRandomItem(_purpleCalculationsService.GetBestPlans(legalStateMoves, gen1ToGen2Evaluations));

            var bestMove =
                bestPlan.ChainedMoves.First();

            _currentBestMove = new EvaluatedChessMove { Move = bestMove.NextMove, Score = bestPlan.EstimatedValue };
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
                var availableStateMovesGen1ToGen2 = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
                var legalStateMovesGen1 = _chessCalculationsService.FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);
                if (legalStateMovesGen1.NextMoves.Count == 1)
                {
                    var onlyMove = legalStateMovesGen1.NextMoves.Single();
                    return new GetNextMoveAnswer(onlyMove.ToString());
                }

                // 2 PLY
                var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                    .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                    .ToDictionary(x => x.Key, x => x.Value);

                var firstGen2Evaluations = legalStateMovesGen1ToGen2.ToDictionary(x => x.Key,
                    x => _purpleCalculationsService.GetBestPlans(x.Value).GetRandomItem(_random));

                var evaluationMessages = legalStateMovesGen1ToGen2
                    .Select(y => new EvaluateMove(y.Key, y.Value));

                var evaluationTasks = evaluationMessages
                    .Select(message => _evaluationRouter.Ask<MoveEvaluated>(message));

                var evaluatedMoves = await Task.WhenAll(evaluationTasks)
                    .ConfigureAwait(false);


                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds)
                    return ReturnBestMove(legalStateMovesGen1, firstGen2Evaluations);



                // 3 PLY

                var workerQuestions = legalStateMovesGen1ToGen2
                    .Select(y => new PurplePlyerPlusWorkerQuestion(y.Key, y.Value));

                var tasks = workerQuestions.Select(q => _evaluationRouter.Ask<PurplePlyerPlusWorkerAnswer>(q));
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

        private async Task<KeyValuePair<PreliminaryStateAndMove, MovePlan>> AggregateSecondGradeAnswers(PurplePlyerPlusWorkerAnswer answer)
        {
            var newQuestions =
                answer.FromPreviousPlyToThisPly.Select(y => new PurplePlyerPlusWorkerQuestion(y.Key, y.Value, true));
            var newTasks = newQuestions.Select(q => _evaluationRouter.Ask<PurplePlyerPlusWorkerAnswer>(q));
            var newAnswers = await Task.WhenAll(newTasks).ConfigureAwait(false);

            ValidateExceptions(newAnswers);

            var firstGen3Evaluations = newAnswers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var newPlan = _purpleCalculationsService
                .GetBestPlans(answer.AvailableStateMoves, firstGen3Evaluations)
                .GetRandomItem(_random);

            return new KeyValuePair<PreliminaryStateAndMove, MovePlan>(answer.StateAndMove, newPlan);
        }

        private GetNextMoveAnswer ReturnBestMove(AvailableStateMoves legalStateMovesGen1,
            Dictionary<PreliminaryStateAndMove, MovePlan> firstGen2Evaluations)
        {
            var bestPlan = _purpleCalculationsService
                .GetBestPlans(legalStateMovesGen1, firstGen2Evaluations)
                .GetRandomItem(_random);
            var bestMove = bestPlan.ChainedMoves.First();
            return new GetNextMoveAnswer(bestMove.NextMove, bestPlan.EstimatedValue);
        }

        private static void ValidateExceptions(PurplePlyerPlusWorkerAnswer[] answers)
        {
            var exception = answers.FirstOrDefault(x => x.ExceptionMessage != null)?.ExceptionMessage;
            if (exception != null) throw new Exception(exception);
        }



        private TValue GetRandomItem<TValue>(ICollection<TValue> values)
        {
            return values.ElementAt(_random.Next(0, values.Count));
        }

        private class EvaluatedChessMove
        {
            public ChessMove Move { get; set; }
            public float Score { get; set; }
        }

        private class MoveSelected
        {
            public MoveSelected(IActorRef sender, ChessMove chosenMove, float moveScore)
            {
                Sender = sender;
                ChosenChessMove = chosenMove;
                MoveScore = moveScore;
            }
            public IActorRef Sender
            {
                get;
                private set;
            }
            public ChessMove ChosenChessMove
            {
                get;
                private set;
            }
            public float MoveScore
            {
                get;
                private set;
            }
        }

        private class MoveSelectionCancelled { }

    }
}