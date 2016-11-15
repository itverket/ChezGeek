using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Actors;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Messages;
using ChezGeek.TeamBrown.Players;
using Geek2k16.Entities;
using Geek2k16.Entities.Constants;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Xunit;

namespace ChezGeek.TeamBrown.Tests
{
    
    public class FembotTest
    {
        [Fact]
        public async Task CheckTimeOfEvaluate()
        {
            var actorSystem = ActorSystem.Create("ChezCluster");
            var player = actorSystem.ActorOf(Props.Create(() => new Player1(Player.White)), "board");
            var chessBoardState = GetInitialState();
            var stopwatch = Stopwatch.StartNew();
            var answer = await player.Ask<GetNextMoveAnswer>(new GetNextMoveQuestion(chessBoardState), new TimeSpan(0,0,30));
            stopwatch.Stop();
            Assert.NotNull(answer);
            Assert.True(stopwatch.Elapsed.Seconds < 30);
        }

        public ChessBoardState GetInitialState()
        {
            return GetStateFromGrid(GridConstants.InitialGrid);
        }

        public ChessBoardState GetStateFromGrid(Abbr?[,] gridArray, Player player = Player.White)
        {
            var chessGrid = SetupChessGrid(gridArray);
            var stateGridHash = GetGridHash(chessGrid);
            var hashHistory = new List<long> { stateGridHash };
            return new ChessBoardState(chessGrid, RuleConstants.StartTime, RuleConstants.StartTime, player, gridHashHistory: hashHistory);
        }

        private static ChessGrid SetupChessGrid(Abbr?[,] abbrGrid)
        {
            var chessGrid = new ChessPiece?[8, 8];
            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var piece = abbrGrid[row, column];
                    if (!piece.HasValue)
                        continue;
                    chessGrid[column, row] = Converters.ChessPieces[piece.Value];
                }
            return new ChessGrid(chessGrid);
        }

        private static long GetGridHash(Abbr?[,] gridArray)
        {
            long hc = gridArray.Length;

            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var abbr = gridArray[row, column];
                    var cellValue = abbr.HasValue ? (int)abbr : -1;
                    hc = unchecked(hc * 314159 + cellValue);
                }

            return hc;
        }

        private static long GetGridHash(ChessGrid chessGrid)
        {
            return GetGridHash(GetAbbreviationGrid(chessGrid));
        }

        private static Abbr?[,] GetAbbreviationGrid(ChessGrid chessGrid)
        {
            var dictionary = Converters.ChessPieces.ToDictionary(x => x.Value, x => x.Key);
            var abbrGrid = new Abbr?[8, 8];
            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var piece = chessGrid[(ChessColumn)column, (ChessRow)row];
                    if (!piece.HasValue)
                        continue;
                    abbrGrid[row, column] = dictionary[piece.Value];
                }
            return abbrGrid;
        }
    }
}
