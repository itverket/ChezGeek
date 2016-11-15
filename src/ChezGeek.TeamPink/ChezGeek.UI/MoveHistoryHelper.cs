using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ChezGeek.Common.Actors;
using Geek2k16.Entities.Enums;

namespace ChezGeek.UI
{
    public static class MoveHistoryHelper
    {
        public static void SetMoveHistory(ChessBoardStateViewModel state, ListView moveHistory)
        {
            if (state.LastMove.HasValue)
            {
                RefreshMoveHistory(state, moveHistory);
                return;
            }
            moveHistory.ItemsSource = null;
        }

        private static void RefreshMoveHistory(ChessBoardStateViewModel state, ItemsControl moveHistoryList)
        {


            var result = state.MoveHistory.Aggregate(new List<string>(), (aggregate, current) =>
            {

                if (current.ChessMove.Player == Player.Black)
                {
                    aggregate[aggregate.Count - 1] += $" {current.Caption}";
                    return aggregate;
                }
                var index = aggregate.Count+ 1;
                aggregate.Add($"{index}. {current.Caption}");
                return aggregate;
            });

            result.Reverse();
            moveHistoryList.ItemsSource = result;
        }


    }
}