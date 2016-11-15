using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Akka.Actor;

using ChezGeek.TeamYellow.Messages;
using ChezGeek.TeamYellow.Services;

using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamYellow.Actors
{
    public class ChessMoveEvaluatorActor : ReceiveActor, IWithUnboundedStash
    {
        private const int DEPTH = 3;

        private readonly YellowCalculationService _chessCalculationsService;

        private readonly Random _random;

        private CancellationTokenSource _cancellationTokenSource;

        public ChessMoveEvaluatorActor()
        {
            _chessCalculationsService = new YellowCalculationService();
            _random = new Random();
            _cancellationTokenSource = new CancellationTokenSource();
            Idle();
        }

        public IStash Stash { get; set; }

        private class EvaluationFinished
        {
            public EvaluationFinished(IActorRef sender, PreliminaryStateAndMove stateAndMove, MovePlan movePlan)
            {
                Sender = sender;
                StateAndMove = stateAndMove;
                MovePlan = movePlan;
            }

            public IActorRef Sender { get; }

            public PreliminaryStateAndMove StateAndMove { get; }

            public MovePlan MovePlan { get; }
        }

        private class EvaluationCancelled
        {
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

        protected override void PreStart()
        {
            Console.WriteLine($"Evaluator {Self.Path.Name} started");
            base.PreStart();
        }

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
            if (unstashMessages) Stash.UnstashAll();
            else Stash.ClearStash();
            Become(Idle);
        }

        private MovePlan Evaluate(
            AvailableStateMoves availableStateMoves,
            CancellationToken cancellationToken,
            Player player,
            PreliminaryStateAndMove stateAndMove,
            int depth)
        {
            if (cancellationToken.IsCancellationRequested) return default(MovePlan);
            //if (stateAndMove.PreliminaryState.LastMove == null) depth--;
            return MiniMaxToMove(player, availableStateMoves, cancellationToken, depth);
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
                    StartEvaluation(message.StateAndMove, message.AvailableStateMoves, Sender, DEPTH);
                });
            Receive<Cancel>(_ => { });
        }

        private EvaluationResult MiniMax(
            Player player,
            float alpha,
            float beta,
            KeyValuePair<PreliminaryStateAndMove, AvailableStateMoves> nextMoves,
            CancellationToken cancellationToken,
            int depth)
        {
            depth--;
            if (cancellationToken.IsCancellationRequested) return new EvaluationResult(beta, alpha, float.MinValue);
            if (depth == 0)
            {
                var score = _chessCalculationsService.EvaluateMove(nextMoves.Key.PreliminaryState.ChessGrid, nextMoves.Key.NextMove);

                return new EvaluationResult(beta, alpha, score);
            }
            var nextPlayerToMove = nextMoves.Value.State.NextToMove;
            var moves = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(nextMoves.Value);
            if (player == nextPlayerToMove)
            {
                foreach (var movePlan in moves)
                {
                    var score = MiniMax(ToOponentPlayer(player), alpha, beta, movePlan, cancellationToken, depth).EstimatedValue;

                    if (score > alpha) alpha = score;
                    if (alpha >= beta) break;
                }

                return new EvaluationResult(beta, alpha, alpha);
            }
            foreach (var movePlan in moves)
            {
                var score = MiniMax(ToOponentPlayer(player), alpha, beta, movePlan, cancellationToken, depth).EstimatedValue;
                if (score < beta) beta = score;
                if (alpha >= beta) break;
            }

            return new EvaluationResult(beta, alpha, beta);
        }

        private MovePlan MiniMaxToMove(Player player, AvailableStateMoves availableStateMoves, CancellationToken cancellationToken, int depth)
        {
            depth--;
            var moves = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMoves);

            var alpha = float.MinValue;
            var beta = float.MaxValue;
            var nextPlayerToMove = availableStateMoves.State.NextToMove;
            var bestMove = new PreliminaryStateAndMove();
            if (player == nextPlayerToMove)
            {
                foreach (var movePlan in moves)
                {
                    var estimatedResult = MiniMax(ToOponentPlayer(player), alpha, beta, movePlan, cancellationToken, depth);
                    alpha = estimatedResult.Alpha;
                    beta = estimatedResult.Beta;
                    var score = estimatedResult.EstimatedValue;
                    if (score > alpha)
                    {
                        alpha = score;
                        bestMove = movePlan.Key;
                    }
                    if (alpha >= beta) break;
                }

                return new MovePlan(new[] { bestMove }, alpha);
            }

            foreach (var movePlan in moves)
            {
                var estimatedResult = MiniMax(ToOponentPlayer(player), alpha, beta, movePlan, cancellationToken, depth);
                alpha = estimatedResult.Alpha;
                beta = estimatedResult.Beta;
                var score = estimatedResult.EstimatedValue;
                if (score < beta)
                {
                    beta = score;
                    bestMove = movePlan.Key;
                }
                if (alpha >= beta) break;
            }

            return new MovePlan(new[] { bestMove }, beta);
        }

        private void StartEvaluation(PreliminaryStateAndMove stateAndMove, AvailableStateMoves availableStateMoves, IActorRef sender, int depth)
        {
            Task.Run(
                () => Evaluate(availableStateMoves, _cancellationTokenSource.Token, stateAndMove.NextMove.Player, stateAndMove, depth),
                _cancellationTokenSource.Token).ContinueWith<object>(
                task =>
                {
                    if (task.IsCanceled || task.IsFaulted) return new EvaluationCancelled();
                    return new EvaluationFinished(sender, stateAndMove, task.Result);
                }).PipeTo(Self);
        }

        private static Player ToOponentPlayer(Player player)
        {
            switch (player)
            {
                case Player.White:
                    return Player.Black;
                case Player.Black:
                    return Player.White;

                default:
                    throw new ArgumentOutOfRangeException(nameof(player), player, null);
            }
        }
    }

    internal class EvaluationResult
    {
        public EvaluationResult(float beta, float alpha, float estimatedValue)
        {
            Beta = beta;
            Alpha = alpha;
            EstimatedValue = estimatedValue;
        }

        public float Beta { get; }

        public float Alpha { get; }

        public float EstimatedValue { get; }
    }
}