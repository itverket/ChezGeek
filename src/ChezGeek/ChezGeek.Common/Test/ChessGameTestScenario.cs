using Akka.Actor;
using Akka.Configuration;
using ChezGeek.Common.Actors;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using Geek2k16.Service;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;

namespace ChezGeek.Common.Test
{
    public class ChessGameTestScenario : IDisposable
    {
        private ActorSystem _actorSystem;
        private ChessCalculationsService _chessCalculationService;
        private TextWriter _output;
        private Type _whitePlayerType;
        private Type _blackPlayerType;
        private int _numberOfRuns;
        private bool _verbose;
        private bool _isDisposed;

        public ChessGameTestScenario(
            Type whitePlayerType, Type blackPlayerType, TextWriter output, int numberOfRuns = 1, bool verbose = false)
        {
            if (!typeof(UntypedActor).IsAssignableFrom(whitePlayerType))
                throw new ArgumentException("White player type must be an Akka.net actor", nameof(whitePlayerType));
            if (!typeof(UntypedActor).IsAssignableFrom(blackPlayerType))
                throw new ArgumentException("Black player type must be an Akka.net actor", nameof(blackPlayerType));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (numberOfRuns < 1)
                throw new ArgumentException("Number of runs must be greater than zero", nameof(numberOfRuns));

            _whitePlayerType = whitePlayerType;
            _blackPlayerType = blackPlayerType;
            _output = output;
            _numberOfRuns = numberOfRuns;
            _verbose = verbose;

            _actorSystem = ActorSystem.Create(
                "ChessGameTestScenario",
                ConfigurationFactory.ParseString("akka.suppress-json-serializer-warning = true"));

            _chessCalculationService = new ChessCalculationsService();
        }

        private static string GetPlayerName(Type whitePlayerType)
        {
            var chessPlayerAttribute = whitePlayerType.GetCustomAttribute<ChessPlayerAttribute>();
            if (chessPlayerAttribute == null)
                return whitePlayerType.FullName;

            return chessPlayerAttribute.PlayerName;
        }

        public async Task RunAsync()
        {
            var testSummary = new TestSummary(
                _numberOfRuns,
                GetPlayerName(_whitePlayerType),
                GetPlayerName(_blackPlayerType));

            for (var run = 0; run < _numberOfRuns; run++)
            {
                await RunScenarioAsync(run, testSummary).ConfigureAwait(false);
                Console.WriteLine($"Ran {run+1} of {_numberOfRuns} games.");
            }

            _output.WriteLine(testSummary.ToString());
        }

        private async Task RunScenarioAsync(int run, TestSummary testSummary)
        {
            var boardActor = _actorSystem.ActorOf(
                Props.Create(() => new BoardActor(_whitePlayerType, _blackPlayerType)));

            //if (_verbose)
            //{
            //    _output.WriteLine($"Game {run}");
            //    _output.WriteLine();
            //}

            var initialStateAnswer = await boardActor.Ask<GetInitialGameStateAnswer>(new GetInitialGameStateQuestion()).ConfigureAwait(false);
            var state = initialStateAnswer.InitialChessBoardState.ChessBoardState;

            while (!state.EndResult.HasValue)
            {
                var gameStateAnswer = await boardActor.Ask<GetNextGameStateAnswer>(new GetNextGameStateQuestion()).ConfigureAwait(false);
                state = gameStateAnswer.ChessBoardState.ChessBoardState;
                Console.WriteLine(Math.Ceiling(state.MoveHistory.Count / 2.0));
                Console.WriteLine(state.ChessGrid.ToString());
                Console.WriteLine(Environment.NewLine);
            }

            //if (_verbose)
            //{
            //    OutputGameSummary(testSummary, state);
            //}

            UpdateTestSummary(state, testSummary);

            await boardActor.GracefulStop(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        private void OutputGameSummary(TestSummary testSummary, ChessBoardState state)
        {
            _output.WriteLine(_chessCalculationService.GetMoveHistoryText(state));
            _output.WriteLine();

            if (state.PlayerWon == Player.White)
                _output.WriteLine($"{testSummary.WhitePlayerSummary.PlayerName} WON!");
            else if (state.PlayerWon == Player.Black)
                _output.WriteLine($"{testSummary.BlackPlayerSummary.PlayerName} WON!");
            else
                _output.WriteLine("REMIS!");

            _output.WriteLine();
            _output.WriteLine($"End result: {state.EndResult}");
            _output.WriteLine();
        }

        private static void UpdateTestSummary(ChessBoardState state, TestSummary testSummary)
        {
            testSummary.WhitePlayerSummary.NumberOfWins += state.PlayerWon == Player.White ? 1 : 0;
            testSummary.WhitePlayerSummary.NumberOfDraws += !state.PlayerWon.HasValue ? 1 : 0;
            testSummary.WhitePlayerSummary.NumberOfLosses += state.PlayerWon == Player.Black ? 1 : 0;

            testSummary.BlackPlayerSummary.NumberOfWins += state.PlayerWon == Player.Black ? 1 : 0;
            testSummary.BlackPlayerSummary.NumberOfDraws += !state.PlayerWon.HasValue ? 1 : 0;
            testSummary.BlackPlayerSummary.NumberOfLosses += state.PlayerWon == Player.White ? 1 : 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                if (_actorSystem != null)
                {
                    _actorSystem.Terminate().Wait();
                    _actorSystem = null;
                }
            }

            _isDisposed = true;
        }

        private class TestSummary
        {
            private int _numberOfRuns;

            public TestSummary(int numberOfRuns, string whitePlayerName, string blackPlayerName)
            {
                _numberOfRuns = numberOfRuns;

                WhitePlayerSummary = new PlayerSummary(whitePlayerName, numberOfRuns);
                BlackPlayerSummary = new PlayerSummary(blackPlayerName, numberOfRuns);
            }

            public PlayerSummary WhitePlayerSummary { get; }
            public PlayerSummary BlackPlayerSummary { get; }

            public override string ToString()
            {
                var builder = new StringBuilder();

                builder.Append($"Chess game test summary{Environment.NewLine}");
                builder.Append($"~~~~~~~~~~~~~~~~~~~~~~~{Environment.NewLine}");
                builder.Append(Environment.NewLine);
                builder.Append($"{WhitePlayerSummary.PlayerName} VS {BlackPlayerSummary.PlayerName} (Runs: {_numberOfRuns})");
                builder.Append(Environment.NewLine + Environment.NewLine);

                builder.Append(WhitePlayerSummary.ToString());
                builder.Append(Environment.NewLine + Environment.NewLine);
                builder.Append(BlackPlayerSummary.ToString());

                return builder.ToString();
            }
        }

        private class PlayerSummary
        {
            private readonly int _numberOfRuns;

            public PlayerSummary(string playerName, int numberOfRuns)
            {
                PlayerName = playerName;

                _numberOfRuns = numberOfRuns;
            }

            public string PlayerName { get; }
            public int NumberOfWins { get; set; }
            public int NumberOfDraws { get; set; }
            public int NumberOfLosses { get; set; }

            private decimal GetWinPercentage()
            {
                return (decimal) NumberOfWins / _numberOfRuns;
            }

            public override string ToString()
            {
                var builder = new StringBuilder();

                builder.Append($"{PlayerName}{Environment.NewLine}");
                builder.Append($"{new string('~', PlayerName.Length)}{Environment.NewLine}");
                builder.Append(Environment.NewLine);
                builder.Append($"Wins: {NumberOfWins}/{_numberOfRuns} ({GetWinPercentage():#.##} %){Environment.NewLine}");
                builder.Append($"Draws: {NumberOfDraws}/{_numberOfRuns}{Environment.NewLine}");
                builder.Append($"Losses: {NumberOfLosses}/{_numberOfRuns}");

                return builder.ToString();
            }
        }
    }
}
