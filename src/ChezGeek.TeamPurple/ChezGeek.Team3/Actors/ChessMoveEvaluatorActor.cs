using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Messages;
using ChezGeek.TeamPurple.Messages;
using ChezGeek.TeamPurple.Services;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace ChezGeek.TeamPurple.Actors
{
    public class ChessMoveEvaluatorActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly PurpleCalculationsService _purpleCalculationsService;
        private readonly Random _random;

        private CancellationTokenSource _cancellationTokenSource;

        public ChessMoveEvaluatorActor()
        {
            _chessCalculationsService = new ChessCalculationsService();
            _purpleCalculationsService = new PurpleCalculationsService(_chessCalculationsService);

            _random = new Random();
            _cancellationTokenSource = new CancellationTokenSource();

            Idle();
        }

        public IStash Stash { get; set; }

        protected override void PreStart()
        {
            Console.WriteLine($"Evaluator {Self.Path.Name} started");

            base.PreStart();
        }

        protected override void PostRestart(Exception reason)
        {
            Console.WriteLine($"Evaluator {Self.Path.Name} restarted");

            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(reason.Message);
            Console.WriteLine(reason.StackTrace);
            Console.ForegroundColor = previousColor;

            base.PostRestart(reason);
        }

        protected override void PostStop()
        {
            Console.WriteLine($"Evaluator {Self.Path.Name} stopped");

            base.PostStop();
        }

        private void Idle()
        {
            Receive<EvaluateMove>(message =>
            {
                Become(Active);
                StartEvaluation(message.StateAndMove, message.AvailableStateMoves, Sender);
            });

            Receive<Cancel>(_ => { });
        }

        private void Active()
        {
            Receive<EvaluationFinished>(evaluation =>
            {
                evaluation.Sender.Tell(new MoveEvaluated(evaluation.StateAndMove, evaluation.MovePlan), Self);
                BecomeIdle(true);
            });
            Receive<EvaluationCancelled>(_ => BecomeIdle(false));

            Receive<Cancel>(_ => _cancellationTokenSource.Cancel());

            ReceiveAny(_ => Stash.Stash());
        }

        private void BecomeIdle(bool unstashMessages)
        {
            _cancellationTokenSource.Dispose(); _cancellationTokenSource = new CancellationTokenSource();

            if (unstashMessages) Stash.UnstashAll(); else Stash.ClearStash();

            Become(Idle);
        }

        private void StartEvaluation(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves, IActorRef sender)
        {
            Task.Run(
                () => Evaluate(availableStateMoves, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token)
                .ContinueWith<object>(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                    return new EvaluationCancelled();

                return new EvaluationFinished(sender, stateAndMove, task.Result);
            }).PipeTo(Self);
        }

        private MovePlan Evaluate(AvailableStateMoves availableStateMoves, CancellationToken cancellationToken)
        {
            var availableStateMovesGen2ToGen3 =
                _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMoves);

            if (cancellationToken.IsCancellationRequested) return default(MovePlan);

            var gen2ToGen3Evaluations = availableStateMovesGen2ToGen3
                .ToDictionary(x => x.Key, x => GetRandomItem(_purpleCalculationsService.GetBestPlans(x.Value)));

            if (cancellationToken.IsCancellationRequested) return default(MovePlan);

            var bestPlan = GetRandomItem(_purpleCalculationsService.GetBestPlans(availableStateMoves, gen2ToGen3Evaluations));
            
            return bestPlan;
        }

        private TValue GetRandomItem<TValue>(ICollection<TValue> values)
        {
            return values.ElementAt(_random.Next(0, values.Count));
        }


        private class EvaluationFinished
        {
            public EvaluationFinished(IActorRef sender, PreliminaryStateAndMove stateAndMove, MovePlan movePlan)
            {
                Sender = sender;
                StateAndMove = stateAndMove;
                MovePlan = movePlan;
            }

            public IActorRef Sender { get; private set; }
            public PreliminaryStateAndMove StateAndMove { get; private set; }
            public MovePlan MovePlan { get; private set; }
        }

        private class EvaluationCancelled { }
    }
}
