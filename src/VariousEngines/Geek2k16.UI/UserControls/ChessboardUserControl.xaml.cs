using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Geek2k16.UI.ViewModels;
using System.Windows.Controls;
using Akka.Actor;
using Geek2k16.Actors;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.UI.Actors;
using Geek2k16.UI.UserControls.Converters;
using PieceType = Geek2k16.Entities.Enums.PieceType;

namespace Geek2k16.UI.UserControls
{
    /// <summary>
    /// Interaction logic for ChessboardUserControl.xaml
    /// </summary>
    public partial class ChessboardUserControl : UserControl
    {
        private readonly Chessboard _viewModel = new Chessboard();
        private readonly IActorRef _board;

        public ChessboardUserControl()
        {
            InitializeComponent();

            var actorSystem = ActorSystem.Create("BoardSystem");

            _board = actorSystem.ActorOf<BoardActor>();
            

            InitializeTiles();
            UpdateUiGrid();
        }

        private async void MakeMove()
        {
            await _board.Ask<ForcePlayerMoveAnswer>(new ForcePlayerMoveQuestion());
            UpdateUiGrid();
            //Update move history
            //Color previous position

            //Check if done
            


        }

        private async void UpdateUiGrid()
        {
            // Position pieces in their new positions.
            var state = await _board.Ask<GetBoardStateAnswer>(new GetBoardStateQuestion());

            var backendBoardPiecePositions = state.State.ChessGrid.GetAllChessPiecePositions().ToList();
            foreach (var tile in _viewModel.Tiles.Values)
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

            // Handle Castling
            if (state.State.LastMove?.ChessMove.PieceType == PieceType.King && state.State.LastMove?.ChessMove.MoveDistance == 2)
            {

            }

            // Colorize previous and current position for the piece that was last moved
            if (!state.State.MoveHistory.Any())
            {
                return;
            }
            RemoveAllOverlays();
            _viewModel.Tiles.Values.First(x => x.ChessPosition == state.State.MoveHistory.Last().ChessMove.FromPosition).OverlayVisible = true;
            _viewModel.Tiles.Values.First(x => x.ChessPosition == state.State.MoveHistory.Last().ChessMove.ToPosition).OverlayVisible = true;
        }

        private void RemoveAllOverlays()
        {
            foreach (var tile in _viewModel.Tiles.Values.Where(x => x.OverlayVisible))
            {
                tile.OverlayVisible = false;
            }
        }

        private static void RemovePieceFromUiTile(Tile tile)
        {
            tile.Piece = null;
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

        private void InitializeTiles()
        {
            foreach (var tile in _viewModel.Tiles.Values)
            {
                Grid.Children.Add(new TileUserControl { ViewModel = tile });
            }
        }

        
        private void MakeMove_OnClick(object sender, RoutedEventArgs e)
        {
            MakeMove();
        }
    }
}
