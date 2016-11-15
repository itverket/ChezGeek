using System;
using System.Collections.Generic;
using System.Linq;
using Geek2k16.Entities.Constants;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace Geek2k16.Entities
{
    [Serializable]
    public struct ChessGrid
    {
        public ChessGrid(ChessPiece?[,] gridArray)
        {
            DoubleArray = gridArray;
        }

        private ChessPiece?[,] DoubleArray { get; }

        public ChessPiece? this[ChessColumn column, ChessRow row] => DoubleArray[(int) column, (int) row];

        public override bool Equals(object obj)
        {
            var chessGrid = obj as ChessGrid?;
            return chessGrid.HasValue && chessGrid.Value == this;
        }

        public static bool operator ==(ChessGrid x, ChessGrid y)
        {
            return x.GetHashCode() == y.GetHashCode();
        }

        public static bool operator !=(ChessGrid x, ChessGrid y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            var hc = 64;
            var abbri = GetAbbreviationGrid();
            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var abbr = abbri[row, column];
                    var cellValue = abbr.HasValue ? (int) abbr : -1;
                    hc = unchecked(hc*314159 + cellValue);
                }

            return hc;
        }

        private Abbr?[,] GetAbbreviationGrid()
        {
            var dictionary = Converters.ChessPieces.ToDictionary(x => x.Value, x => x.Key);
            var abbrGrid = new Abbr?[8, 8];
            for (var row = 0; row < 8; row++)
                for (var column = 0; column < 8; column++)
                {
                    var piece = this[(ChessColumn) column, (ChessRow) row];
                    if (!piece.HasValue)
                        continue;
                    abbrGrid[row, column] = dictionary[piece.Value];
                }
            return abbrGrid;
        }

        public override string ToString()
        {
            var array = DoubleArray;
            var squares = IterateOverAllSquares((c, r) => array[(int) c, (int) r]?.ToString() ?? "[   ]");
            return string.Join(Environment.NewLine, squares.Select(x => string.Join("", x)));
        }

        public IEnumerable<ChessPiecePosition> GetAllChessPiecePositions()
        {
            var array = DoubleArray;
            return IterateOverAllSquares(
                (c, r) => new {piece = array[(int) c, (int) r], position = new ChessPosition(c, r)})
                .SelectMany(x => x).Where(x => x.piece != null)
                .Select(x => new ChessPiecePosition(x.piece.Value, x.position));
        }

        private static IEnumerable<IEnumerable<TResult>> IterateOverAllSquares<TResult>(
            Func<ChessColumn, ChessRow, TResult> function)
        {
            return Enumerable.Range(0, 8)
                .Select(r => Enumerable.Range(0, 8).Select(c => function((ChessColumn) c, (ChessRow) r)));
        }
    }
}