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
using ChezGeek.TeamRed.Geek2k16.Service;
using ChezGeek.TeamRed.Messages;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using POS = Geek2k16.Entities.Enums.PieceType;
using COL = Geek2k16.Entities.Enums.ChessColumn;
using ROW = Geek2k16.Entities.Enums.ChessRow;

namespace ChezGeek.TeamRed.Actors
{
    [ChessPlayer("DeepRed")]
    public class TeamRedPlayerActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;

        private CancellationTokenSource _cancellationTokenSource;
        private IActorRef _evaluationRouter;
        private Player _player;

        public TeamRedPlayerActor(Player player)
        {
            _player = player;
            _cancellationTokenSource = new CancellationTokenSource();
            _chessCalculationsService = new ChessCalculationsService();
            _random = new Random();

            Idle();
        }

        public IStash Stash { get; set; }

        private void Idle()
        {
            Receive<GetNextMoveQuestion>(question =>
            {
                Become(Active);
                StartGetNextMove(question.ChessBoardState, Sender);
            });
        }

        private void Active()
        {
            Receive<MoveSelected>(moveSelected =>
            {
                moveSelected.Sender.Tell(
                    new GetNextMoveAnswer(moveSelected.ChosenChessMove, moveSelected.MoveScore),
                    Self);
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

        private void StartGetNextMove(ChessBoardState state, IActorRef sender)
        {
            Task.Run(() => GetNextMove(state, _cancellationTokenSource.Token), _cancellationTokenSource.Token)
                .ContinueWith<object>(task =>
                {
                    if (task.IsCanceled || task.IsFaulted) return new MoveSelectionCancelled();

                    return new MoveSelected(sender, task.Result.Move, task.Result.Score);
                }).PipeTo(Self);
        }

        private TValue GetRandomItem<TValue>(ICollection<TValue> values)
        {
            return values.ElementAt(_random.Next(0, values.Count));
        }

        private MovePlan GetHighestEvaluatedItem(ICollection<MovePlan> value)
        {
            return value.First(x => Math.Abs(x.EstimatedValue - value.Max(y => y.EstimatedValue)) <= 0);
        }

        private async Task<EvaluatedChessMove> GetNextMove(ChessBoardState state, CancellationToken cancellationToken)
        {
            if(_player == Player.White)
            {
                var whiteMove1 = new ChessMove(Player.White, POS.Pawn, COL.E, ROW.Row2, COL.E, ROW.Row4);
                var whiteMove2 = new ChessMove(Player.White, POS.Bishop, COL.F, ROW.Row1, COL.C, ROW.Row4);
                var whiteMove3 = new ChessMove(Player.White, POS.Queen, COL.D, ROW.Row1, COL.F, ROW.Row3);
                var whiteMove4 = new ChessMove(Player.White, POS.Queen, COL.F, ROW.Row3, COL.F, ROW.Row7);

                var badMoves = new List<String>
            {
                "f5","f6","e6","d5","b5","Nf6", "Rh6", "Qd5","Qf5", "Nh6"
            };
                var goForScholar = true;
                if (!state.LastMove.HasValue)
                {
                    return new EvaluatedChessMove { Move = whiteMove1 };
                }
                for (int i = 0; i < state.MoveHistory.Count; i++)
                {
                    if (badMoves.Contains(state.MoveHistory.ElementAt(i).Caption))
                    {
                        goForScholar = false;
                    }

                }
                if (state.MoveHistory.Count < 7 && goForScholar)
                {

                    if (state.MoveHistory.ElementAt(0).ChessMove.Equals(whiteMove1) && state.MoveHistory.Count < 3)
                    {
                        if (!badMoves.Contains(state.MoveHistory.ElementAt(1).Caption))
                        {
                            return new EvaluatedChessMove { Move = whiteMove2 };
                        }

                    }
                    if (state.MoveHistory.ElementAt(2).ChessMove.Equals(whiteMove2) && state.MoveHistory.Count < 5)
                    {
                        if (!badMoves.Contains(state.MoveHistory.ElementAt(1).Caption) && !badMoves.Contains(state.MoveHistory.ElementAt(3).Caption))
                        {
                            return new EvaluatedChessMove { Move = whiteMove3 };
                        }
                    }
                    if (state.MoveHistory.ElementAt(4).ChessMove.Equals(whiteMove3) && state.MoveHistory.Count < 7)
                    {
                        if (!badMoves.Contains(state.MoveHistory.ElementAt(1).Caption) && !badMoves.Contains(state.MoveHistory.ElementAt(3).Caption)
                            && !badMoves.Contains(state.MoveHistory.ElementAt(5).Caption))
                        {
                            return new EvaluatedChessMove { Move = whiteMove4 };
                        }
                    }
                }
            }
           
            
            var rootState = new PreliminaryBoardState(state);

            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService.FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

            if (IsOnlyOneLegalMove(legalStateMovesGen1))
            {
                return ReturnFirstMove(state, legalStateMovesGen1);
            }

            var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2.Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove)).ToDictionary(x => x.Key, x => x.Value);

            var evaluationMessages = legalStateMovesGen1ToGen2.Select(y => new EvaluateMove(y.Key, y.Value));
            var evaluationTasks = evaluationMessages.Select(message => _evaluationRouter.Ask<MoveEvaluated>(message));
            var evaluatedMoves = await Task.WhenAll(evaluationTasks).ConfigureAwait(false);

            var gen1ToGen2Evaluations = evaluatedMoves.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var bestPlans = _chessCalculationsService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations);
            var bestPlan = GetRandomItem(bestPlans);
            var bestMove = bestPlan.ChainedMoves.First();

            

            return new EvaluatedChessMove {Move = bestMove.NextMove, Score = bestPlan.EstimatedValue};
        }

        private EvaluatedChessMove ReturnFirstMove(ChessBoardState state, AvailableStateMoves legalStateMovesGen1)
        {
            var onlyMove = legalStateMovesGen1.NextMoves.Single();
            return new EvaluatedChessMove
            {
                Move = onlyMove,
                Score = _chessCalculationsService.GetValueOfPieces(state)
            };
        }

        private static bool IsOnlyOneLegalMove(AvailableStateMoves legalStateMovesGen1)
        {
            return legalStateMovesGen1.NextMoves.Count == 1;
        }


        protected override void PreStart()
        {
            _evaluationRouter = Context.ActorOf(Props.Create<ChessMoveEvaluatorActor>().WithRouter(new ClusterRouterPool(new RoundRobinPool(16), new ClusterRouterPoolSettings(16, 4, false, "node"))));

            base.PreStart();
        }

        private class EvaluatedChessMove
        {
            public ChessMove Move { get; set; }
            public float Score { get; set; }
        }

        private class MoveSelectionCancelled
        {
        }

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
    }
}