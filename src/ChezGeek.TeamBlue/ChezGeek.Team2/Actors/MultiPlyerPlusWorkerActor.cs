using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Messages;
using ChezGeek.Team2;
using ChezGeek.Team2.Messages;
using ChezGeek.TeamBlue.Services;

namespace ChezGeek.TeamBlue.Actors
{
    public class MultiPlyerPlusWorkerActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;
        private CancellationTokenSource _cancellationTokenSource;

        public MultiPlyerPlusWorkerActor()
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            _cancellationTokenSource = new CancellationTokenSource();
            Idle();
        }

        protected override void PreStart()
        {
            Console.WriteLine("Starting MultiPlyerPlusWorkerActor");
            base.PreStart();
        }

        protected override void PostStop()
        {
            Console.WriteLine("Stopping MultiPlyerPlusWorkerActor");
            base.PostStop();
        }

        private void Idle()
        {
            Receive<MultiPlyerPlusWorkerQuestion>(question => GetBestMoves(question, Sender));
        }

        private void GetBestMoves(MultiPlyerPlusWorkerQuestion question, IActorRef sender)
        {
            Become(Active);
            Task.Run(() => BestMoves(question), _cancellationTokenSource.Token).ContinueWith<object>(task =>
            {
                if (task.IsCanceled)
                {
                    return new WorkerCancelled();
                }
                if (task.IsFaulted)
                {
                    //new message
                    return new MultiPlyerPlusWorkerAnswer(task.Exception.Message);
                }
                return new BestMoveFound(task.Result, sender);
            }).PipeTo(Self);
        }

        private MultiPlyerPlusWorkerAnswer BestMoves(MultiPlyerPlusWorkerQuestion question)
        {
            var previousPly = question.AvailableStateMoves;
            var fromPreviousPlyToThisPly = _chessCalculationsService
                .GetAvailableMovesWithNextPlyOfAvailableMoves(previousPly);
            var thisPlyEvaluations = fromPreviousPlyToThisPly.ToDictionary(x => x.Key,
                x => _chessCalculationsService.GetBestPlans(x.Value).GetRandomItem(_random));
            var bestPlan = _chessCalculationsService
                .GetBestPlans(previousPly, thisPlyEvaluations)
                .GetRandomItem(_random);
            return new MultiPlyerPlusWorkerAnswer(question, bestPlan, fromPreviousPlyToThisPly);
        }

        private void Active()
        {
            Receive<BestMoveFound>(message =>
            {
                message.Sender.Tell(message.TaskResult);
                BecomeIdle(false);
            });
            Receive<WorkerCancelled>(_ => BecomeIdle(true));
            Receive<Cancel>(_ =>
            {
                _cancellationTokenSource.Cancel();
            });
            ReceiveAny(_ => Stash.Stash());
        }

        private void BecomeIdle(bool isCancel)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            if (isCancel)
            {
                Stash.ClearStash();
            }
            else
            {
                Stash.UnstashAll();
            }
            Become(Idle);
        }

        public IStash Stash { get; set; }
        private class WorkerCancelled
        {
        }

        private class BestMoveFound
        {
            public MultiPlyerPlusWorkerAnswer TaskResult { get; }
            public IActorRef Sender { get; }

            public BestMoveFound(MultiPlyerPlusWorkerAnswer taskResult, IActorRef sender)
            {
                TaskResult = taskResult;
                Sender = sender;
            }
        }
    }


}