using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.TeamPink.Messages;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamPink.Actors
{
    public class ChessMoveEvaluatorActor : ReceiveActor, IWithUnboundedStash
    {
        private class Cancel {}

        private class EvaluationCancelled {}

        private class EvaluationFinished
        {
            public EvaluationFinished(IActorRef sender, PreliminaryStateAndMove stateAndMove, MovePlan movePlan)
            {
                Sender = sender;
                StateAndMove = stateAndMove;
                MovePlan = movePlan;
            }

            #region Public Properties

            public MovePlan MovePlan { get; }

            public IActorRef Sender { get; }
            public PreliminaryStateAndMove StateAndMove { get; }

            #endregion
        }

        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;
        private CancellationTokenSource _cancellationTokenSource;

        public ChessMoveEvaluatorActor()
        {
            _chessCalculationsService = new ChessCalculationsService();
            _random = new Random();
            _cancellationTokenSource = new CancellationTokenSource();

            Idle();
        }

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

        #region Private Methods

        private void Active()
        {
            Receive<EvaluationFinished>(
                evaluation =>
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
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            if (unstashMessages)
                Stash.UnstashAll();
            else
                Stash.ClearStash();

            Become(Idle);
        }

        private MovePlan Evaluate(AvailableStateMoves availableStateMoves, TimeSpan timeLeft, CancellationToken cancellationToken)
        {
            var availableStateMovesGen2ToGen3 =
                _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMoves);

            if (cancellationToken.IsCancellationRequested)
                return default(MovePlan);

            // TEST

            Dictionary<PreliminaryStateAndMove, MovePlan> gen2ToGen3Evaluations;

            if (timeLeft > new TimeSpan(0, 2, 0) && false)
            {
                var legalStateMovesGen2 = _chessCalculationsService.FilterToLegalStateMoves(
                    availableStateMovesGen2ToGen3,
                    availableStateMoves);

                if (cancellationToken.IsCancellationRequested)
                    return default(MovePlan);

                var legalStateMovesGen2ToGen3 = availableStateMovesGen2ToGen3
                    .Where(x => legalStateMovesGen2.NextMoves.Contains(x.Key.NextMove))
                    .ToDictionary(x => x.Key, x => x.Value);

                if (cancellationToken.IsCancellationRequested)
                    return default(MovePlan);

                var availableStateMovesGen3ToGen4 = legalStateMovesGen2ToGen3.Values
                    .AsParallel()
                    .Select(x => _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(x))
                    .ToArray();

                if (cancellationToken.IsCancellationRequested)
                    return default(MovePlan);

                var gen3ToGen4Evaluations = availableStateMovesGen3ToGen4
                    .AsParallel()
                    .Select(
                        x =>
                            x.ToDictionary(
                                y => y.Key,
                                y => GetRandomItem(_chessCalculationsService.GetBestPlans(y.Value))))
                    .SelectMany(x => x)
                    .ToDictionary(x => x.Key, x => x.Value);

                if (cancellationToken.IsCancellationRequested)
                    return default(MovePlan);

                gen2ToGen3Evaluations = availableStateMovesGen2ToGen3
                    .ToDictionary(
                        x => x.Key,
                        x => GetRandomItem(_chessCalculationsService.GetBestPlans(x.Value, gen3ToGen4Evaluations)));
            }
            else
            {
                gen2ToGen3Evaluations = availableStateMovesGen2ToGen3.ToDictionary(
                    x => x.Key,
                    x => GetRandomItem(_chessCalculationsService.GetBestPlans(x.Value)));

            }

            // TEST

            if (cancellationToken.IsCancellationRequested)
                return default(MovePlan);

            var bestPlan =
                GetRandomItem(_chessCalculationsService.GetBestPlans(availableStateMoves, gen2ToGen3Evaluations));

            return bestPlan;
        }

        private TValue GetRandomItem<TValue>(ICollection<TValue> values)
        {
            return values.ElementAt(_random.Next(0, values.Count));
        }

        private void Idle()
        {
            Receive<EvaluateMove>(
                message =>
                {
                    Become(Active);
                    StartEvaluation(message.StateAndMove, message.AvailableStateMoves, message.TimeLeft, Sender);
                });

            Receive<Cancel>(_ => { });
        }

        private void StartEvaluation(
            PreliminaryStateAndMove stateAndMove,
            AvailableStateMoves availableStateMoves,
            TimeSpan timeLeft,
            IActorRef sender)
        {
            Task.Run(
                () => Evaluate(availableStateMoves, timeLeft, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token).ContinueWith<object>(
                task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                        return new EvaluationCancelled();

                    return new EvaluationFinished(sender, stateAndMove, task.Result);
                }).PipeTo(Self);
        }

        #endregion

        #region IActorStash Members

        public IStash Stash { get; set; }

        #endregion
    }
}