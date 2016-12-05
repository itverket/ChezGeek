using System;
using Geek2k16.Service;

namespace Geek2k16.FindMoves
{
    internal class Program
    {
        /// + Calculate Checks + Stalemate
        private static void Main(string[] args)
        {
            var moveService = new ChessCalculationsService();
            var initialState = moveService.GetInitialState();
            Console.WriteLine(initialState);
            var moves = moveService.GetLegalMoves(initialState);
            var movesText = string.Join(Environment.NewLine, moves);
            Console.WriteLine(movesText);
        }
    }
}