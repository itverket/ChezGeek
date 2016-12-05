using Geek2k16.Entities.Enums;
using System;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct ChessPiece
    {
        public ChessPiece(PieceType pieceType, Player player)
        {
            Player = player;
            PieceType = pieceType;
        }

        public PieceType PieceType { get; private set; }
        public Player Player { get; private set; }

        public override string ToString()
        {
            var player = Player.ToString().Substring(0, 1);
            var pieceType = PieceType.ToString().Substring(0, 2);
            return $"[{player}{pieceType}]";
        }
    }
}