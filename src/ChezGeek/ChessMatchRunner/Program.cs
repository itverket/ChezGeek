using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Test;

namespace ChessMatchRunner
{
    class Program
    {

        static void Main(string[] args)
        {
            var gameId = Guid.NewGuid().ToString().Substring(0,5);
            using (var writer = new StreamWriter($"Results\\GameResults-{gameId}.txt"))
            {
                var aviablePlayers = ChessPlayerHelper.GetPlayers().ToList();
                Console.WriteLine($"Starting match with players: {aviablePlayers[0]} vs {aviablePlayers[1]}");
                var gameRunner = new ChessGameTestScenario(aviablePlayers[0], aviablePlayers[1], writer, 3, true);
                var runningGames = gameRunner.RunAsync();
                Task.WaitAll(runningGames);
            }
        }
    }
}
