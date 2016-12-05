using Geek2k16.Entities.Structs;

namespace Geek2k16.Actors
{
    public class GetNextMoveAnswer : IAnswer
    {
        //ChessMove

        public ChessMove ChessMove { get; set; }

        public GetNextMoveAnswer(ChessMove chessMove)
        {
            ChessMove = chessMove;
        }
    }
}