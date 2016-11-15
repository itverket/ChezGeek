using System.Collections.Generic;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace Geek2k16.Entities.Constants
{
    public static class LegalMoves
    {
        public static Dictionary<PieceType, PositionOffset[]> PieceOffsets =
            new Dictionary<PieceType, PositionOffset[]>
            {
                {PieceType.Pawn, ForPawn()},
                {PieceType.Bishop, ForBishop()},
                {PieceType.King, ForKing()},
                {PieceType.Knight, ForKnight()},
                {PieceType.Rook, ForRook()},
                {PieceType.Queen, ForQueen()}
            };

        public static PositionOffset[] ForPawn()
        {
            return new[]
            {
                new PositionOffset(0, -1, MoveCondition.CannotCapture | MoveCondition.DoubleMoveIfStartRow),
                new PositionOffset(1, -1, MoveCondition.MustCapture),
                new PositionOffset(-1, -1, MoveCondition.MustCapture)
            };
        }

        public static PositionOffset[] ForKnight()
        {
            return new[]
            {
                new PositionOffset(2, -1, MoveCondition.MoveThroughPieces),
                new PositionOffset(2, 1, MoveCondition.MoveThroughPieces),
                new PositionOffset(-2, 1, MoveCondition.MoveThroughPieces),
                new PositionOffset(-2, -1, MoveCondition.MoveThroughPieces),
                new PositionOffset(-1, 2, MoveCondition.MoveThroughPieces),
                new PositionOffset(-1, -2, MoveCondition.MoveThroughPieces),
                new PositionOffset(1, 2, MoveCondition.MoveThroughPieces),
                new PositionOffset(1, -2, MoveCondition.MoveThroughPieces)
            };
        }

        public static PositionOffset[] ForKing()
        {
            return new[]
            {
                new PositionOffset(1, 1),
                new PositionOffset(1, -1),
                new PositionOffset(-1, 1),
                new PositionOffset(-1, -1),
                new PositionOffset(0, 1),
                new PositionOffset(0, -1),
                new PositionOffset(1, 0),
                new PositionOffset(-1, 0),
                new PositionOffset(2, 0, MoveCondition.CastleShort),
                new PositionOffset(-2, 0, MoveCondition.CastleLong)
            };
        }


        public static PositionOffset[] ForQueen()
        {
            return new[]
            {
                new PositionOffset(1, 1, MoveCondition.UnlimitedDistance),
                new PositionOffset(1, -1, MoveCondition.UnlimitedDistance),
                new PositionOffset(-1, 1, MoveCondition.UnlimitedDistance),
                new PositionOffset(-1, -1, MoveCondition.UnlimitedDistance),
                new PositionOffset(0, 1, MoveCondition.UnlimitedDistance),
                new PositionOffset(0, -1, MoveCondition.UnlimitedDistance),
                new PositionOffset(1, 0, MoveCondition.UnlimitedDistance),
                new PositionOffset(-1, 0, MoveCondition.UnlimitedDistance)
            };
        }


        public static PositionOffset[] ForRook()
        {
            return new[]
            {
                new PositionOffset(0, 1, MoveCondition.UnlimitedDistance),
                new PositionOffset(0, -1, MoveCondition.UnlimitedDistance),
                new PositionOffset(1, 0, MoveCondition.UnlimitedDistance),
                new PositionOffset(-1, 0, MoveCondition.UnlimitedDistance)
            };
        }


        public static PositionOffset[] ForBishop()
        {
            return new[]
            {
                new PositionOffset(1, 1, MoveCondition.UnlimitedDistance),
                new PositionOffset(1, -1, MoveCondition.UnlimitedDistance),
                new PositionOffset(-1, 1, MoveCondition.UnlimitedDistance),
                new PositionOffset(-1, -1, MoveCondition.UnlimitedDistance)
            };
        }
    }
}