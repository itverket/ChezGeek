using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace Geek2k16.Actors
{
    public class GetLegalMoveSetAnswer : IAnswer
    {
        //ChessMove[]
        public ChessMove[] LegalMoves { get; set; }


        public GetLegalMoveSetAnswer(ChessMove[] legalMoves)
        {
            LegalMoves = legalMoves;
        }
    }
}