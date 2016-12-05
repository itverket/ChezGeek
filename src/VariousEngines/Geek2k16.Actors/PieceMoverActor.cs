using System;
using System.Threading.Tasks;
using Akka.Actor;
using Geek2k16.Entities;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;

namespace Geek2k16.Actors
{
    public class PieceMoverActor : ReceiveActor
    {
        private ChessCalculationsService _chessMoveService;

        public PieceMoverActor()
        {
            _chessMoveService = new ChessCalculationsService();
            Receive<UpdateStateQuestion>(q => Sender.Tell(UpdateStateQuestion(q), Self));

        }

        private UpdateStateAnswer UpdateStateQuestion(UpdateStateQuestion q)
        {
            var newBoard = _chessMoveService.GetStateAfterMove(q.ChessBoardState, new ExecutedChessMove(q.ChessMove, new TimeSpan()));
            return new UpdateStateAnswer(newBoard);
        }
    }

    public class UpdateStateAnswer
    {
        public UpdateStateAnswer(ChessBoardState state)
        {
            ChessBoardState = state;
        }

        public ChessBoardState ChessBoardState { get; set; }
    }

    public class UpdateStateQuestion
    {
        public UpdateStateQuestion(ChessBoardState state, ChessMove move)
        {
            ChessMove = move;
            ChessBoardState = state;
        }

        public ChessBoardState ChessBoardState { get; set; }

        public ChessMove ChessMove { get; set; }
    }
}