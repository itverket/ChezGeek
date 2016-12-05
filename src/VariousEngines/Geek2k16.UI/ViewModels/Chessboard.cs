using System.Collections.Generic;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace Geek2k16.UI.ViewModels
{
    public class Chessboard : ObservableObject
    {
        private readonly Dictionary<string, Tile> _tiles = new Dictionary<string, Tile>();


        public Chessboard()
        {
            Reset();
        }

        public IReadOnlyDictionary<string, Tile> Tiles => _tiles;

        private void Reset()
        {
            _tiles.Clear();

            TileColor tileColor = TileColor.Light;

            ChessColumn[] columnHeaders =
            {
                ChessColumn.A, 
                ChessColumn.B,
                ChessColumn.C,
                ChessColumn.D,
                ChessColumn.E,
                ChessColumn.F,
                ChessColumn.G,
                ChessColumn.H
            };

            ChessRow[] rowHeaders =
{
                ChessRow.Row1,
                ChessRow.Row2,
                ChessRow.Row3,
                ChessRow.Row4,
                ChessRow.Row5,
                ChessRow.Row6,
                ChessRow.Row7,
                ChessRow.Row8
            };


            for (int row = 0; row < 8; row++)
            {
                tileColor = tileColor == TileColor.Light
                    ? TileColor.Dark
                    : TileColor.Light;

                for (int column = 0; column < 8; column++)
                {
                    tileColor = tileColor == TileColor.Light
                    ? TileColor.Dark
                    : TileColor.Light;

                    var tileNumber = $"{columnHeaders[column]}{8 - row}";
                    var tilePosition = new ChessPosition(columnHeaders[column], rowHeaders[7 - row]);
                    _tiles.Add(tileNumber, new Tile
                    {
                        ChessPosition = tilePosition,
                        Number = tileNumber,
                        TileColor = tileColor,
                        Piece = _initialPiecePositions.ContainsKey(tilePosition)
                            ? _initialPiecePositions[tilePosition]
                            : null
                    });
                }
            }
        }

        private static IReadOnlyDictionary<ChessPosition, Piece> _initialPiecePositions = new Dictionary<ChessPosition, Piece>
        {
            [new ChessPosition(ChessColumn.A, ChessRow.Row1)] = new Piece { PieceType = PieceType.Rook,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.B, ChessRow.Row1)] = new Piece { PieceType = PieceType.Knight,  PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.C, ChessRow.Row1)] = new Piece { PieceType = PieceType.Bishop,  PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.D, ChessRow.Row1)] = new Piece { PieceType = PieceType.Queen,   PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.E, ChessRow.Row1)] = new Piece { PieceType = PieceType.King,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.F, ChessRow.Row1)] = new Piece { PieceType = PieceType.Bishop,  PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.G, ChessRow.Row1)] = new Piece { PieceType = PieceType.Knight,  PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.H, ChessRow.Row1)] = new Piece { PieceType = PieceType.Rook,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.A, ChessRow.Row2)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.B, ChessRow.Row2)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.C, ChessRow.Row2)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.D, ChessRow.Row2)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.E, ChessRow.Row2)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.F, ChessRow.Row2)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.G, ChessRow.Row2)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.White },
            [new ChessPosition(ChessColumn.H, ChessRow.Row2)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.White },

            [new ChessPosition(ChessColumn.A, ChessRow.Row8)] = new Piece { PieceType = PieceType.Rook,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.B, ChessRow.Row8)] = new Piece { PieceType = PieceType.Knight,  PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.C, ChessRow.Row8)] = new Piece { PieceType = PieceType.Bishop,  PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.D, ChessRow.Row8)] = new Piece { PieceType = PieceType.Queen,   PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.E, ChessRow.Row8)] = new Piece { PieceType = PieceType.King,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.F, ChessRow.Row8)] = new Piece { PieceType = PieceType.Bishop,  PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.G, ChessRow.Row8)] = new Piece { PieceType = PieceType.Knight,  PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.H, ChessRow.Row8)] = new Piece { PieceType = PieceType.Rook,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.A, ChessRow.Row7)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.B, ChessRow.Row7)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.C, ChessRow.Row7)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.D, ChessRow.Row7)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.E, ChessRow.Row7)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.F, ChessRow.Row7)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.G, ChessRow.Row7)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.Black },
            [new ChessPosition(ChessColumn.H, ChessRow.Row7)] = new Piece { PieceType = PieceType.Pawn,    PieceColor = PieceColor.Black }
        };

    }
}
