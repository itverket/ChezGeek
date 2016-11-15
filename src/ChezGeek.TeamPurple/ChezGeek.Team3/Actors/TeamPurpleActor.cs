using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Routing;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using ChezGeek.TeamPurple.Messages;
using ChezGeek.TeamPurple.Services;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;
using ChezGeek.TeamPurple.Openings;

namespace ChezGeek.TeamPurple.Actors
{
    [ChessPlayer("Deep Purple")]
    public class TeamPurpleActor : ReceiveActor, IWithUnboundedStash
    {
        private const int NumberOfWorkerActors = 40;
        private const int NumberOfActorsPerNode = 8;
        private const int DivideRemainingTimeBy = 30;

        private readonly Player _player;
        private readonly ChessCalculationsService _chessCalculationsService;
        private CancellationTokenSource _cancellationTokenSource;
        private PurpleCalculationsService _purpleCalculationsService;
        private readonly Random _random;
        private IOpening _opening;
        public IStash Stash { get; set; }

        private IActorRef _evaluationRouter;
        private readonly IActorRef _workerRouter;

        public TeamPurpleActor(Player player)
        {
            _player = player;
            _cancellationTokenSource = new CancellationTokenSource();
            _chessCalculationsService = new ChessCalculationsService();
            _purpleCalculationsService = new PurpleCalculationsService(_chessCalculationsService);
            _random = new Random();

            _workerRouter = Context.ActorOf(Props.Create<PurplePlyerPlusWorkerActor>()
               .WithRouter(new RoundRobinPool(NumberOfActorsPerNode)));

            _opening = new OpeningFactory().GetOpening();

            Idle();
        }
        protected override void PreStart()
        {
            _evaluationRouter = Context.ActorOf(Props.Create<ChessMoveEvaluatorActor>()
                .WithRouter(new ClusterRouterPool(new SmallestMailboxPool(NumberOfWorkerActors),
                new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, false, "node"))));

            base.PreStart();
        }
        private void Idle()
        {
            Receive<GetNextMoveQuestion>(question =>
            {
                Become(Active);
                StartGetNextMove(question.ChessBoardState, Sender);
            });
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

        private void StartGetNextMove(ChessBoardState state, IActorRef sender)
        {
            Task.Run(() => GetNextMove(state, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token)
                .ContinueWith<object>(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                        return new MoveSelectionCancelled();

                    return new MoveSelected(sender, task.Result.Move, task.Result.Score);
                }).PipeTo(Self);
        }

        private async Task<EvaluatedChessMove> GetNextMove(ChessBoardState state, CancellationToken cancellationToken)
        {

            var stopwatch = Stopwatch.StartNew();

            var remainingTime = state.NextToMove == Player.Black ? state.BlackTime : state.WhiteTime;
            var timeLimitSeconds = remainingTime.TotalSeconds / DivideRemainingTimeBy;

            var rootState = new PreliminaryBoardState(state);



            // LEGAL MOVES
            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService.FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

            if (_opening != null)
            {            
                var openingMove = _opening.GetNextMove(state);
                if (openingMove.MoveDistance > 0)
                {
                    if (legalStateMovesGen1.NextMoves.Contains(openingMove))
                    {

                        return new EvaluatedChessMove
                        {
                            Move = openingMove,
                            Score = _chessCalculationsService.GetValueOfPieces(state)
                        };
                    }
                    else
                    {
                        _opening = null;
                    }
                }
            }

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


            var gen1ToGen2Evaluations = evaluatedMoves
                .ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var bestPlan =
                GetRandomItem(_purpleCalculationsService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations));

            var bestMove =
                bestPlan.ChainedMoves.First();

            return new EvaluatedChessMove { Move = bestMove.NextMove, Score = bestPlan.EstimatedValue };
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