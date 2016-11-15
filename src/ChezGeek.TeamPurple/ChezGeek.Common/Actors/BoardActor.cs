using System;
using System.Collections.Generic;
using Akka.Actor;
using ChezGeek.Common.Messages;
using System.Threading.Tasks;
using Geek2k16.Entities;
using Geek2k16.Entities.Structs;
using Geek2k16.Entities.Enums;
using System.Diagnostics;

namespace ChezGeek.Common.Actors
{
    public class BoardActor : ReceiveActor
    {
        private readonly IActorRef _arbiterActor;
        private IActorRef _whitePlayerActor;
        private IActorRef _blackPlayerActor;
        private Stopwatch _stopwatch;

        private ChessBoardState _chessBoardState;

        public BoardActor(Type whitePlayerActorType, Type blackPlayerActorType)
        {
            _whitePlayerActor = Context.ActorOf(Props.Create(whitePlayerActorType, Player.White), "white-player");
            _blackPlayerActor = Context.ActorOf(Props.Create(blackPlayerActorType, Player.Black), "black-player");

            _arbiterActor = Context.ActorOf<ArbiterActor>();

            _stopwatch = new Stopwatch();

            Init();
        }

        private void Init()
        {
            ReceiveAsync<GetInitialGameStateQuestion>(async _ =>
            {
                var initialChessBoardState = await GetInitialChessBoardState();

                _chessBoardState = initialChessBoardState;

                Sender.Tell(new GetInitialGameStateAnswer(initialChessBoardState));

                Become(Ready);
            });
        }

        private async Task<ChessBoardState> GetInitialChessBoardState()
        {
            var initialBoardStateAnswer = await _arbiterActor.Ask<GetInitialBoardStateAnswer>(
                new GetInitialBoardStateQuestion());

            return initialBoardStateAnswer.InitialBoardState;
        }

        private void Ready()
        {
            ReceiveAsync<GetNextGameStateQuestion>(async _ => Sender.Tell(await GetNextGameState(), Self));
        }

        private async Task<GetNextGameStateAnswer> GetNextGameState()
        {
            var nextChessBoardState = await Step();

            return new GetNextGameStateAnswer(nextChessBoardState);
        }

        private async Task<ChessBoardStateViewModel> Step()
        {
            _stopwatch.Reset();
            _stopwatch.Start();

            GetNextMoveAnswer move;
            try
            {
                move = await AskForNextMove();
                if (move.ExceptionMessage != null)
                {
                    return new ChessBoardStateViewModel(_chessBoardState, $"{_chessBoardState.NextToMove} experienced an exception: {move.ExceptionMessage}");
                }
            }
            catch (TaskCanceledException e)
            {
                return new ChessBoardStateViewModel(_chessBoardState, $"{_chessBoardState.NextToMove} ran out of time!");
            }
            _stopwatch.Stop();

            var executedMove = new ExecutedChessMove(move.ChessMove, _stopwatch.Elapsed);

            if (await IsLegalMove(new IsLegalMoveQuestion(_chessBoardState, move.ChessMove)))
            {
                _chessBoardState = await GetNextChessBoardState(executedMove);
            }

            return new ChessBoardStateViewModel(_chessBoardState, move.PerceivedStrength);
        }

        private async Task<GetNextMoveAnswer> AskForNextMove()
        {
            var getNextMoveQuestion = new GetNextMoveQuestion(_chessBoardState);

            GetNextMoveAnswer nextMoveAnswer;


            if (_chessBoardState.NextToMove == Player.White)
            {
                nextMoveAnswer = await _whitePlayerActor.Ask<GetNextMoveAnswer>(getNextMoveQuestion, _chessBoardState.WhiteTime);
            }
            else
            {
                nextMoveAnswer = await _blackPlayerActor.Ask<GetNextMoveAnswer>(getNextMoveQuestion, _chessBoardState.BlackTime);
            }
            return nextMoveAnswer;
        }

        private async Task<bool> IsLegalMove(IsLegalMoveQuestion question)
        {
            var isLegalMoveAnswer = await _arbiterActor.Ask<IsLegalMoveAnswer>(question);

            return isLegalMoveAnswer.IsLegal;
        }

        private async Task<ChessBoardState> GetNextChessBoardState(ExecutedChessMove move)
        {
            var nextChessBoardStateAnswer = await _arbiterActor.Ask<GetNextBoardStateAnswer>(
                new GetNextBoardStateQuestion(_chessBoardState, move));

            return nextChessBoardStateAnswer.ChessBoardState;
        }
    }

    public class ChessBoardStateViewModel
    {
        public ChessBoardStateViewModel(ChessBoardState chessBoardState, float perceivedStrength = 0)
        {
            BlackTime = chessBoardState.BlackTime;
            WhiteTime = chessBoardState.WhiteTime;
            PlayerWon = chessBoardState.PlayerWon;
            ChessGrid = chessBoardState.ChessGrid;
            EndResult = chessBoardState.EndResult;
            LastMove = chessBoardState.LastMove;
            MoveHistory = chessBoardState.MoveHistory;
            NextToMove = chessBoardState.NextToMove;
            LastPerceivedStrength = perceivedStrength > 0 ? Math.Min(perceivedStrength, 50) : Math.Max(perceivedStrength, -50);

            ChessBoardState = chessBoardState;
        }

        public ChessBoardStateViewModel(ChessBoardState chessBoardState, string playerOutOfTimeText)
        {
            BlackTime = chessBoardState.BlackTime;
            WhiteTime = chessBoardState.WhiteTime;
            PlayerWon = chessBoardState.PlayerWon;
            ChessGrid = chessBoardState.ChessGrid;
            EndResult = chessBoardState.EndResult;
            LastMove = chessBoardState.LastMove;
            MoveHistory = chessBoardState.MoveHistory;
            NextToMove = chessBoardState.NextToMove;
            LastPerceivedStrength = 0;

            OutofTime = true;
            OutOfTimeText = playerOutOfTimeText;

            ChessBoardState = chessBoardState;

        }

        public string OutOfTimeText { get; set; }

        public bool OutofTime { get; set; }

        public ChessBoardState ChessBoardState { get; }

        public float LastPerceivedStrength { get; set; }

        public Player NextToMove { get; set; }

        public IReadOnlyCollection<LoggedChessMove> MoveHistory { get; set; }

        public LoggedChessMove? LastMove { get; set; }

        public StateResult? EndResult { get; set; }

        public ChessGrid ChessGrid { get; set; }

        public Player? PlayerWon { get; set; }

        public TimeSpan WhiteTime { get; set; }

        public TimeSpan BlackTime { get; set; }
    }
}
