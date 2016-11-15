using Geek2k16.Entities.Enums;

namespace Geek2k16.Entities.Structs
{
    public interface IPreliminaryBoardState
    {
        StateFlag? StateFlags { get; }
        ChessGrid ChessGrid { get; }
        Player NextToMove { get; }
        LoggedChessMove? LastMove { get; }
    }
}