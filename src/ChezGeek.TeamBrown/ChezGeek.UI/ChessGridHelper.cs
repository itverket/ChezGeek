using System.Collections.Generic;
using System.Linq;
using ChezGeek.Common.Actors;
using ChezGeek.UI.UserControls.Converters;
using ChezGeek.UI.ViewModels;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.UI
{
    public static class ChessGridHelper
    {
        public static void UpdateUiGrid(ChessBoardStateViewModel state, List<Tile> tiles)
        {
            // Position pieces in their new positions.

            var backendBoardPiecePositions = state.ChessGrid.GetAllChessPiecePositions().ToList();
            foreach (var tile in tiles)
            {
                if (backendBoardPiecePositions.Any(piece => piece.ChessPosition == tile.ChessPosition))
                {
                    PlacePieceOnUiTile(backendBoardPiecePositions, tile);
                }
                else
                {
                    RemovePieceFromUiTile(tile);
                }
            }
            if (!state.MoveHistory.Any())
            {
                return;
            }
            HighlightLastMove(state, tiles);
        }
        private static void PlacePieceOnUiTile(List<ChessPiecePosition> currentChessPiecePositions, Tile tile)
        {
            var newChessPiece =
                currentChessPiecePositions.First(piece => piece.ChessPosition == tile.ChessPosition).ChessPiece;

            var pieceColor = newChessPiece.Player == Player.White ? PieceColor.White : PieceColor.Black;
            var pieceType = newChessPiece.PieceType.Map();

            if (tile.Piece != null && tile.Piece.PieceColor == pieceColor && tile.Piece.PieceType == pieceType)
            {
                return;
            }

            tile.Piece = new Piece
            {
                PieceColor = pieceColor,
                PieceType = pieceType
            };
        }

        private static void HighlightLastMove(ChessBoardStateViewModel state, List<Tile> tiles)
        {
            RemoveAllHighlights(tiles);
            if (state.MoveHistory.Any())
            {
                tiles.First(x => x.ChessPosition == state.MoveHistory.Last().ChessMove.FromPosition).Highlight = true;
                tiles.First(x => x.ChessPosition == state.MoveHistory.Last().ChessMove.ToPosition).Highlight = true;
            }
        }

        public static void RemoveAllHighlights(IEnumerable<Tile> tiles)
        {
            foreach (var tile in tiles.Where(x => x.Highlight))
            {
                tile.Highlight = false;
            }
        }

        private static void RemovePieceFromUiTile(Tile tile)
        {
            tile.Piece = null;
        }
    }
}