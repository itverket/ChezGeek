using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Routing;

using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using ChezGeek.TeamYellow.Messages;
using ChezGeek.TeamYellow.Services;

using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

using Cancel = ChezGeek.Common.Messages.Cancel;

namespace ChezGeek.TeamYellow.Actors
{
    [ChessPlayer("Connecting ...")]
    public class TeamYellowPlayerActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly YellowCalculationService _chessCalculationsService;

        private readonly Player _player;

        private readonly Random _random;

        private CancellationTokenSource _cancellationTokenSource;

        private IActorRef _evaluationRouter;

        public TeamYellowPlayerActor(Player player)
        {
            _player = player;
            _cancellationTokenSource = new CancellationTokenSource();
            _chessCalculationsService = new YellowCalculationService();
            _random = new Random();
            Idle();
        }

        public IStash Stash { get; set; }

        private class MoveSelected
        {
            public MoveSelected(IActorRef sender, ChessMove chosenMove, float moveScore)
            {
                Sender = sender;
                ChosenChessMove = chosenMove;
                MoveScore = moveScore;
            }

            public IActorRef Sender { get; }

            public ChessMove ChosenChessMove { get; }

            public float MoveScore { get; }
        }

        private class MoveSelectionCancelled
        {
        }

        private class EvaluatedChessMove
        {
            public ChessMove Move { get; set; }

            public float Score { get; set; }
        }

        protected override void PreStart()
        {
            _evaluationRouter =
                Context.ActorOf(
                    Props.Create<ChessMoveEvaluatorActor>()
                        .WithRouter(new ClusterRouterPool(new RoundRobinPool(16), new ClusterRouterPoolSettings(16, 4, false, "node"))));
            base.PreStart();
        }

        private void Active()
        {
            Receive<MoveSelected>(
                moveSelected =>
                {
                    moveSelected.Sender.Tell(new GetNextMoveAnswer(moveSelected.ChosenChessMove, moveSelected.MoveScore), Self);
                    BecomeIdle();
                });
            Receive<MoveSelectionCancelled>(_ => BecomeIdle());
            Receive<Cancel>(_ => _cancellationTokenSource.Cancel());
            ReceiveAny(_ => Stash.Stash());
        }

        private void BecomeIdle()
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            Stash.UnstashAll();
            Become(Idle);
        }

        private async Task<EvaluatedChessMove> GetNextMove(ChessBoardState state, CancellationToken cancellationToken)
        {
            var rootState = new PreliminaryBoardState(state);
            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService.FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);
            if (legalStateMovesGen1.NextMoves.Count == 1)
            {
                var onlyMove = legalStateMovesGen1.NextMoves.Single();
                return new EvaluatedChessMove { Move = onlyMove, Score = _chessCalculationsService.GetValueOfPieces(state) };
            }
            var legalStateMovesGen1ToGen2 =
                availableStateMovesGen1ToGen2.Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove)).ToDictionary(x => x.Key, x => x.Value);
            var evaluationMessages = legalStateMovesGen1ToGen2.Select(y => new EvaluateMove(y.Key, y.Value));
            var evaluationTasks = evaluationMessages.Select(message => _evaluationRouter.Ask<MoveEvaluated>(message));
            var evaluatedMoves = await Task.WhenAll(evaluationTasks).ConfigureAwait(false);
            var gen1ToGen2Evaluations = evaluatedMoves.ToDictionary(x => x.StateAndMove, x => x.MovePlan);
            var bestPlan = GetRandomItem(_chessCalculationsService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations));
            var bestMove = bestPlan.ChainedMoves.First();
            return new EvaluatedChessMove { Move = bestMove.NextMove, Score = bestPlan.EstimatedValue };
        }

        private TValue GetRandomItem<TValue>(ICollection<TValue> values)
        {
            return values.ElementAt(_random.Next(0, values.Count));
        }

        private void Idle()
        {
            Receive<GetNextMoveQuestion>(
                question =>
                {
                    Become(Active);
                    StartGetNextMove(question.ChessBoardState, Sender);
                });
        }

        private void StartGetNextMove(ChessBoardState state, IActorRef sender)
        {
            Task.Run(() => GetNextMove(state, _cancellationTokenSource.Token), _cancellationTokenSource.Token).ContinueWith<object>(
                task =>
                {
                    if (task.IsCanceled || task.IsFaulted) return new MoveSelectionCancelled();
                    return new MoveSelected(sender, task.Result.Move, task.Result.Score);
                }).PipeTo(Self);
        }
    }
}