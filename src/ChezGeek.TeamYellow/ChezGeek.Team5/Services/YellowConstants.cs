using System.Collections.Generic;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamYellow.Services
{
    public static class YellowConstants
    {
        public static List<ChessPosition> DeepCenter = new List<ChessPosition>()
        {
            new ChessPosition(ChessColumn.D, ChessRow.Row4),
            new ChessPosition(ChessColumn.D, ChessRow.Row5),
            new ChessPosition(ChessColumn.E, ChessRow.Row4),
            new ChessPosition(ChessColumn.E, ChessRow.Row5)
        };

        public static List<ChessPosition> OuterCenter = new List<ChessPosition>()
        {
            new ChessPosition(ChessColumn.C, ChessRow.Row3),
            new ChessPosition(ChessColumn.C, ChessRow.Row4),
            new ChessPosition(ChessColumn.C, ChessRow.Row5),
            new ChessPosition(ChessColumn.C, ChessRow.Row6),
            new ChessPosition(ChessColumn.D, ChessRow.Row3),
            new ChessPosition(ChessColumn.D, ChessRow.Row6),
            new ChessPosition(ChessColumn.E, ChessRow.Row3),
            new ChessPosition(ChessColumn.E, ChessRow.Row6),
            new ChessPosition(ChessColumn.F, ChessRow.Row3),
            new ChessPosition(ChessColumn.F, ChessRow.Row4),
            new ChessPosition(ChessColumn.F, ChessRow.Row5),
            new ChessPosition(ChessColumn.F, ChessRow.Row6),
        };

        public static List<ChessPosition> Row2 = new List<ChessPosition>()
        {
            new ChessPosition(ChessColumn.A, ChessRow.Row2),
            new ChessPosition(ChessColumn.B, ChessRow.Row2),
            new ChessPosition(ChessColumn.C, ChessRow.Row2),
            new ChessPosition(ChessColumn.D, ChessRow.Row2),
            new ChessPosition(ChessColumn.E, ChessRow.Row2),
            new ChessPosition(ChessColumn.F, ChessRow.Row2),
            new ChessPosition(ChessColumn.G, ChessRow.Row2),
            new ChessPosition(ChessColumn.H, ChessRow.Row2),
        };
        public static List<ChessPosition> Row3 = new List<ChessPosition>()
        {
            new ChessPosition(ChessColumn.A, ChessRow.Row3),
            new ChessPosition(ChessColumn.B, ChessRow.Row3),
            new ChessPosition(ChessColumn.C, ChessRow.Row3),
            new ChessPosition(ChessColumn.D, ChessRow.Row3),
            new ChessPosition(ChessColumn.E, ChessRow.Row3),
            new ChessPosition(ChessColumn.F, ChessRow.Row3),
            new ChessPosition(ChessColumn.G, ChessRow.Row3),
            new ChessPosition(ChessColumn.H, ChessRow.Row3),
        };
    }
}