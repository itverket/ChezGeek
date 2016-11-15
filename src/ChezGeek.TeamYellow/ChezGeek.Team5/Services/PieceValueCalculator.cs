using System.Linq;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamYellow.Services
{
    public static class PieceValueCalculator
    {
        public static float GetPawnValue(ChessPiecePosition chessPiecePosition, float pieceValue)
        {
            if (YellowConstants.DeepCenter.Any(x => x.Equals(chessPiecePosition.ChessPosition)))
            {
                pieceValue = pieceValue * (float)1.02;
            }
            if (YellowConstants.OuterCenter.Any(x => x.Equals(chessPiecePosition.ChessPosition)))
            {
                pieceValue = pieceValue * (float)1.01;
            }
            return pieceValue;
        }

        public static float GetKnightValue(ChessPiecePosition chessPiecePosition, float pieceValue)
        {
            if (YellowConstants.DeepCenter.Any(x => x.Equals(chessPiecePosition.ChessPosition)))
            {
                pieceValue = pieceValue * (float)1.01;
            }
            if (YellowConstants.OuterCenter.Any(x => x.Equals(chessPiecePosition.ChessPosition)))
            {
                pieceValue = pieceValue * (float)1.005;
            }
            return pieceValue;
        }

        public static float GetKingValue(ChessPiecePosition chessPiecePosition, float pieceValue)
        {
            if (YellowConstants.Row2.Any(x => x.Equals(chessPiecePosition.ChessPosition)))
            {
                pieceValue = pieceValue*(float) 0.98;
            }
            if (YellowConstants.Row3.Any(x => x.Equals(chessPiecePosition.ChessPosition)))
            {
                pieceValue = pieceValue * (float)0.97;
            }
            return pieceValue;
        }
        public static float GetQueenValue(ChessPiecePosition chessPiecePosition, float pieceValue)
        {
            return pieceValue;
        }
        public static float GetBishopValue(ChessPiecePosition chessPiecePosition, float pieceValue)
        {
            
            return pieceValue;
        }
        public static float GetRookValue(ChessPiecePosition chessPiecePosition, float pieceValue)
        {

            return pieceValue;
        }
    }
}