using Geek2k16.Entities.Enums;
using System;

namespace Geek2k16.Entities.Structs
{
    [Serializable]
    public struct ChessPosition
    {
        public bool Equals(ChessPosition other)
        {
            return Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ChessPosition && Equals((ChessPosition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Column*397) ^ (int) Row;
            }
        }

        public ChessPosition(ChessColumn column, ChessRow row)
        {
            Column = column;
            Row = row;
        }

        public ChessColumn Column { get; private set; }
        public ChessRow Row { get; private set; }

        public override string ToString()
        {
            return $"{Column}:{Row}";
        }

        public static bool operator ==(ChessPosition cp1, ChessPosition cp2)
        {
            return cp1.Equals(cp2);
        }

        public static bool operator !=(ChessPosition cp1, ChessPosition cp2)
        {
            return !(cp1 == cp2);
        }
    }
}