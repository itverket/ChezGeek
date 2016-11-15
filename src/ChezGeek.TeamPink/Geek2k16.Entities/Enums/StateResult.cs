namespace Geek2k16.Entities.Enums
{
    public enum StateResult
    {
        WhiteKingCheckmated,
        BlackKingCheckmated,
        WhiteIsOutOfTime,
        BlackIsOutOfTime,
        RepeatStateThreeTimes,
        Stalemate,
        WhiteKingChecked,
        BlackKingChecked,
        InsufficientMaterial,
        FiftyInconsequentialMoves,
        WhiteIllegalMove,
        BlackIllegalMove
    }
}