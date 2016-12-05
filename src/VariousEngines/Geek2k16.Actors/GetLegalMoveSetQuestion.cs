using Geek2k16.Entities;
using Geek2k16.Entities.Structs;

namespace Geek2k16.Actors
{
    public class GetLegalMoveSetQuestion : IQuestion
    {
        public ChessBoardState ChessBoardState { get; set; }

        public GetLegalMoveSetQuestion(ChessBoardState chessBoardState)
        {
            ChessBoardState = chessBoardState;
        }
    }

    public class IsLegalMoveQuestion : IQuestion
    {
        public ChessBoardState ChessBoardState { get; set; }
        public ChessMove ChessMove { get; set; }

        public IsLegalMoveQuestion(ChessBoardState chessBoardState, ChessMove chessMove)
        {
            ChessBoardState = chessBoardState;
            ChessMove = chessMove;
        }
    }

    public class IsLegalMoveAnswer : IAnswer
    {
        public bool IsLegal { get; set; }
        public IsLegalMoveAnswer(bool isLegal)
        {
            IsLegal = isLegal;
        }
    }
}