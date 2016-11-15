namespace ChezGeek.UI.ViewModels
{
    public class Piece : ObservableObject
    {
        private PieceType _pieceType;
        private PieceColor _pieceColor;

        public PieceType PieceType
        {
            get { return _pieceType; }
            set
            {
                _pieceType = value;

                RaisePropertyChangedEvent(nameof(PieceType));
            }
        }

        public PieceColor PieceColor
        {
            get { return _pieceColor; }
            set
            {
                _pieceColor = value;

                RaisePropertyChangedEvent(nameof(PieceColor));
            }
        }
    }

    public enum PieceColor
    {
        White,
        Black
    }

    public enum PieceType
    {
        Undefined,
        Bishop,
        King,
        Knight,
        Pawn,
        Queen,
        Rook
    }
}
