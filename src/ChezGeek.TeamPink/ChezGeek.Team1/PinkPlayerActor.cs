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
using ChezGeek.TeamPink.Actors;
using ChezGeek.TeamPink.Messages;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamPink
{
    [ChessPlayer("KillCarlsen")]
    public class PinkPlayerActor : ReceiveActor, IWithUnboundedStash
    {
        private class Cancel {}

        private class EvaluatedChessMove
        {
            #region Public Properties

            public ChessMove Move { get; set; }
            public float Score { get; set; }

            #endregion
        }

        private class MoveSelected
        {
            public MoveSelected(IActorRef sender, ChessMove chosenMove, float moveScore)
            {
                Sender = sender;
                ChosenChessMove = chosenMove;
                MoveScore = moveScore;
            }

            #region Public Properties

            public ChessMove ChosenChessMove { get; }
            public float MoveScore { get; }
            public IActorRef Sender { get; }

            #endregion
        }

        private class MoveSelectionCancelled {}

        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Player _player;
        private readonly Random _random;
        private CancellationTokenSource _cancellationTokenSource;
        private IActorRef _evaluationRouter;
        private static string _lastStaticMove;
        private static bool _skipStaticMove;

        public PinkPlayerActor(Player player)
        {
            _player = player;
            _cancellationTokenSource = new CancellationTokenSource();
            _chessCalculationsService = new ChessCalculationsService();
            _random = new Random();
            Idle();
        }

        #region Protected Methods

        protected override void PreStart()
        {
            _evaluationRouter = Context.ActorOf(
                Props.Create<ChessMoveEvaluatorActor>()
                    .WithRouter(
                        new ClusterRouterPool(
                            new RoundRobinPool(50),
                            new ClusterRouterPoolSettings(50, 3, true, "node"))));

            base.PreStart();
        }

        #endregion

        #region Private Methods

        private void Active()
        {
            Receive<MoveSelected>(
                moveSelected =>
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

        private async Task<Dictionary<PreliminaryStateAndMove, MovePlan>> EvaluateOnOtherNodes(
            Dictionary<PreliminaryStateAndMove, AvailableStateMoves> legalStateMovesGen1ToGen2,
            TimeSpan playedTime)
        {
            var evaluationMessages = legalStateMovesGen1ToGen2.Select(y => new EvaluateMove(y.Key, y.Value, playedTime));

            var evaluationTasks = evaluationMessages.Select(message => _evaluationRouter.Ask<MoveEvaluated>(message));

            var evaluatedMoves = await Task.WhenAll(evaluationTasks).ConfigureAwait(false);
            var gen1ToGen2Evaluations = evaluatedMoves.ToDictionary(x => x.StateAndMove, x => x.MovePlan);
            return gen1ToGen2Evaluations;
        }

        private async Task<EvaluatedChessMove> GetNextMove(ChessBoardState state, CancellationToken cancellationToken)
        {
            var move = GetNextStaticMoveWhite(state)
                       ?? GetNextStaticMoveBlack(state);
            if (move != null)
            {
                return move;
            }

            var rootState = new PreliminaryBoardState(state);

            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 =
                _chessCalculationsService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService.FilterToLegalStateMoves(
                availableStateMovesGen1ToGen2,
                availableStateMovesGen1);

            if (legalStateMovesGen1.NextMoves.Count == 1)
            {
                var onlyMove = legalStateMovesGen1.NextMoves.Single();
                return new EvaluatedChessMove
                {
                    Move = onlyMove,
                    Score = _chessCalculationsService.GetValueOfPieces(state)
                };
            }

            var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2.
                Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove)).
                ToDictionary(x => x.Key, x => x.Value);

            var playedTime = GetPlayTime(state);
            var gen1ToGen2Evaluations = await EvaluateOnOtherNodes(legalStateMovesGen1ToGen2, playedTime);

            var bestPlan =
                GetRandomItem(_chessCalculationsService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations));

            var bestMove = bestPlan.ChainedMoves.First();

            return new EvaluatedChessMove
            {
                Move = bestMove.NextMove,
                Score = bestPlan.EstimatedValue
            };
        }

        private EvaluatedChessMove GetNextStaticMoveBlack(ChessBoardState state)
        {
            if (state.NextToMove != Player.Black)
            {
                return null;
            }

            if (state.MoveHistory.Count == 1)
            {
                return MoveIfValid(state, "Black|Pawn|D7|D5");
            }
            if (state.MoveHistory.Count == 3)
            {
                return MoveIfValid(state, "Black|Pawn|E7|E6");
            }
            if (state.MoveHistory.Count == 5)
            {
                return MoveIfValid(state, "Black|Knight|B8|A6");
            }
            if (state.MoveHistory.Count == 7)
            {
                return MoveIfValid(state, "Black|Pawn|H7|H5");
            }
            if (state.MoveHistory.Count == 9)
            {
                return MoveIfValid(state, "Black|Rook|H8|H6");
            }

            return null;
        }

        private EvaluatedChessMove GetNextStaticMoveWhite(ChessBoardState state)
        {
            if (state.NextToMove != Player.White)
            {
                return null;
            }

            if (state.MoveHistory.Count == 0)
            {
                _skipStaticMove = false;
                return MoveIfValid(state, "White|Pawn|E2|E3");
            }
            if (state.MoveHistory.Count == 2)
            {
                var pieceAtToPlace = state.ChessGrid[ChessColumn.D, ChessRow.Row5];
                if (pieceAtToPlace.HasValue)
                {
                    _skipStaticMove = true;
                    return null;
                }
                return MoveIfValid(state, "White|Bishop|F1|C4");
            }
            if (state.MoveHistory.Count == 4)
            {
                var pieceAtToPlace = state.ChessGrid[ChessColumn.F, ChessRow.Row6];
                if (pieceAtToPlace.HasValue && pieceAtToPlace.Value.PieceType == PieceType.Knight)
                {
                    _skipStaticMove = false;
                    return null;
                }
                return MoveIfValid(state, "White|Queen|D1|H5");
            }
            if (state.MoveHistory.Count == 6)
            {
                return MoveIfValid(state, "White|Bishop|C4|F7");
            }

            return null;
        }

        private EvaluatedChessMove GetNextStaticMoveWhite2(ChessBoardState state)
        {
            if (state.NextToMove != Player.White)
            {
                return null;
            }

            if (state.MoveHistory.Count == 0)
            {
                return MoveIfValid(state, "White|Pawn|D2|D4");
            }
            if (state.MoveHistory.Count == 2)
            {
                return MoveIfValid(state, "White|Pawn|E2|E3");
            }
            if (state.MoveHistory.Count == 4)
            {
                return MoveIfValid(state, "White|Knight|B1|A3");
            }
            if (state.MoveHistory.Count == 6)
            {
                return MoveIfValid(state, "White|Pawn|H2|H4");
            }
            if (state.MoveHistory.Count == 8)
            {
                return MoveIfValid(state, "White|Rook|H1|H3");
            }

            return null;
        }

        private TimeSpan GetPlayTime(ChessBoardState state)
        {
            if (state.NextToMove == Player.White)
            {
                return state.WhiteTime;
            }
            if (state.NextToMove == Player.Black)
            {
                return state.BlackTime;
            }

            return TimeSpan.Zero;
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

        private EvaluatedChessMove MoveIfValid(ChessBoardState state, string move)
        {
            if (_skipStaticMove)
            {
                return null;
            }

            if (_lastStaticMove == move)
            {
                _skipStaticMove = true;
                return null;
            }

            try
            {
                var player = (Player) Enum.Parse(typeof(Player), move.Split('|')[0]);
                var piece = (PieceType) Enum.Parse(typeof(PieceType), move.Split('|')[1]);
                var fromColumn = (ChessColumn) Enum.Parse(typeof(ChessColumn), move.Split('|')[2].Substring(0, 1));
                var fromRow = (ChessRow) Enum.Parse(typeof(ChessRow), "Row" + move.Split('|')[2].Substring(1, 1));
                var toColumn = (ChessColumn) Enum.Parse(typeof(ChessColumn), move.Split('|')[3].Substring(0, 1));
                var toRow = (ChessRow) Enum.Parse(typeof(ChessRow), "Row" + move.Split('|')[3].Substring(1, 1));

                var chessMove = new ChessMove(
                    new ChessPiecePosition(player, piece, fromColumn, fromRow),
                    new ChessPosition(toColumn, toRow));

                var legalMoves = _chessCalculationsService.GetLegalMoves(state);
                if (legalMoves.Contains(chessMove))
                {
                    _lastStaticMove = move;
                    return new EvaluatedChessMove {Move = chessMove};
                }

                _skipStaticMove = true;
                return null;
            }
            catch (Exception)
            {
                _skipStaticMove = true;
                return null;
            }
        }

        private void StartGetNextMove(ChessBoardState state, IActorRef sender)
        {
            Task.Run(() => GetNextMove(state, _cancellationTokenSource.Token), _cancellationTokenSource.Token)
                .ContinueWith<object>(
                    task =>
                    {
                        if (task.IsCanceled || task.IsFaulted)
                            return new MoveSelectionCancelled();

                        return new MoveSelected(sender, task.Result.Move, task.Result.Score);
                    }).PipeTo(Self);
        }

        #endregion

        #region IActorStash Members

        public IStash Stash { get; set; }

        #endregion
    }
}