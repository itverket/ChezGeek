using Geek2k16.Entities.Structs;

namespace Geek2k16.UI.ViewModels
{
    public class Tile : ObservableObject
    {
        private string _number;
        private ChessPosition _chessPosition;
        private TileColor _tileColor;
        private Piece _piece;
        private double _overlayOpacity;
        private bool _overlayVisible;

        public string Number
        {
            get { return _number; }
            set
            {
                _number = value;

                RaisePropertyChangedEvent(nameof(Number));
            }
        }

        public TileColor TileColor
        {
            get { return _tileColor; }
            set
            {
                _tileColor = value;

                RaisePropertyChangedEvent(nameof(TileColor));
            }
        }


        public ChessPosition ChessPosition
        {
            get { return _chessPosition; }
            set
            {
                _chessPosition = value;

                RaisePropertyChangedEvent(nameof(ChessPosition));
            }
        }


        public Piece Piece
        {
            get { return _piece; }
            set
            {
                _piece = value;

                RaisePropertyChangedEvent(nameof(Piece));
            }
        }

        public bool OverlayVisible
        {
            get { return _overlayVisible; }
            set
            {
                _overlayVisible = value;
                _overlayOpacity = _overlayVisible ? .8 : 0.0;
                RaisePropertyChangedEvent(nameof(OverlayOpacity));
                RaisePropertyChangedEvent(nameof(OverlayVisible));
            }
        }

        public double OverlayOpacity => _overlayOpacity;
    }

    public enum TileColor
    {
        Light,
        Dark,
    }


}
