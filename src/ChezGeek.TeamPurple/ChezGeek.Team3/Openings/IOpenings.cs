using Geek2k16.Entities;
using Geek2k16.Entities.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChezGeek.TeamPurple.Openings
{
    internal interface IOpening
    {
        ChessMove GetNextMove(ChessBoardState chessBoardState);
    }
}
