using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Routing;
using ChezGeek.Common.Messages;
using ChezGeek.TeamBrown.Actors;
using ChezGeek.TeamBrown.Services;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace ChezGeek.TeamBrown.Players
{
    
    public abstract class Master : ReceiveActor, IWithUnboundedStash
    {
        private readonly Player _player;
        private readonly IActorRef _randomMoveActor;
        protected CancellationTokenSource _cancellationTokenSource;
        protected readonly FembotChessCalculationsService ChessCalculationService;
        protected IActorRef _evaluationRouter;
        private Random _random;

        public IStash Stash { get; set; }

        public Master(Player player)
        {
            _player = player;
            _cancellationTokenSource = new CancellationTokenSource();
            ChessCalculationService = new FembotChessCalculationsService();
            _random = new Random();
            Idle();
        }

        private void Idle()
        {
            Receive<GetNextMoveQuestion>(question =>
            {
                Become(Active);
                StartGetNextMove(question.ChessBoardState, Sender);
            });
        }


        protected abstract Task<EvaluatedChessMove> GetNextMove(ChessBoardState state, CancellationToken cancellationToken);

        private void Active()
        {
            Receive<MoveSelected>(moveSelected =>
            {
                moveSelected.Sender.Tell(new GetNextMoveAnswer(moveSelected.ChosenMove, moveSelected.MoveScore), Self);
                Become(Idle);
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

        protected virtual void StartGetNextMove(ChessBoardState state, IActorRef sender)
        {
            Task.Run(() => GetNextMove(state, _cancellationTokenSource.Token), _cancellationTokenSource.Token)
                .ContinueWith<object>(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                        return new MoveSelectionCancelled();

                    return new MoveSelected(sender, task.Result.Move, task.Result.Score);
                })
                .PipeTo(Self);
        }

        protected override void PreStart()
        {
#if DEBUG
            //_evaluationRouter = Context.ActorOf(Props.Create<ChessMoveEvaluatorActor>().WithRouter(new RoundRobinPool(4)));
            _evaluationRouter = Context.ActorOf(Props.Create<ChessMoveEvaluatorActor>().WithRouter(new ClusterRouterPool(new RoundRobinPool(4), new ClusterRouterPoolSettings(12, 4, false, "node"))));
#else
            _evaluationRouter = Context.ActorOf(Props.Create<ChessMoveEvaluatorActor>().WithRouter(new ClusterRouterPool(new RoundRobinPool(80), new ClusterRouterPoolSettings(80, 4, false, "node"))));
#endif
            base.PreStart();
        }

        

        protected TValue GetRandomItem<TValue>(ICollection<TValue> values)
        {
            return values.ElementAt(_random.Next(0, values.Count));
        }

    }

    internal class Cancel
    {
    }

    public class EvaluatedChessMove
    {
        public ChessMove Move { get; set; }
        public float Score { get; set; }
    }

    public class MoveSelected
    {
        public IActorRef Sender { get; set; }
        public ChessMove ChosenMove { get; set; }
        public float MoveScore { get; set; }

        public MoveSelected(IActorRef sender, ChessMove chosenMove, float moveScore)
        {
            Sender = sender;
            ChosenMove = chosenMove;
            MoveScore = moveScore;
        }
    }

    public class MoveSelectionCancelled
    {

    }
}