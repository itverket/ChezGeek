using System.Threading.Tasks;
using Akka.Actor;
using Geek2k16.Actors;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.UI.ViewModels;

namespace Geek2k16.UI.Actors
{
    // TODO: DELETE THIS ACTOR
    // TODO: MOVE UI PROJECT TO ChezGeek.UI
    // TODO: REFERENCE ChezGeek.Common
    // TODO: USE NEW BoardActor in ChezGeek.Common.Actors
    // TODO: THEN EVERYTHING SHOULD WORK
    // TODO: NOTE! Only GetInitialGameState[Question/Answer] and GetNextGameState[Question/Answer] should flow between UI and BoardActor
    public class BoardActor: ReceiveActor
    {
        private ChessBoardState _chessBoardState;
        private readonly IActorRef _arbiter;
        private readonly IActorRef _whitePlayer;
        private readonly IActorRef _blackPlayer;
        private readonly IActorRef _pieceMover;

        private readonly IActorRef _dllListener;
        private Chessboard _viewModel;

        // Created by GUI or Program - handles coordination around the board (players, game flow and rules)
        // Access to GUI-methods, to update visuals
        public BoardActor()
        {
            _dllListener = Context.ActorOf<DllListenerActor>();
            _arbiter = Context.ActorOf<ArbiterActor>();
            _pieceMover = Context.ActorOf<PieceMoverActor>();
            _whitePlayer = Context.ActorOf<TestPlayerActor>();
            _blackPlayer = Context.ActorOf<TestPlayerActor>();

            InitializeBoardState();

            ReceiveAsync<ForcePlayerMoveQuestion>(async q => Sender.Tell(await QueryPlayerForMove(), Self));
            Receive<GetBoardStateQuestion>(x => Sender.Tell(new GetBoardStateAnswer {State = _chessBoardState}, Self));

            //TODO: Query DllListener
            //TODO: Query Auditor when found two players

            //WaitingForViewModel();
        }

        //private void WaitingForViewModel()
        //{
        //    Receive<Chessboard>(x =>
        //    {
        //        _viewModel = x;
        //        Become(UpdateViewModel);
        //    });
        //}

        //private void UpdateViewModel()
        //{
        //    Receive<>(x =>
        //    {

        //    });
        //}


        private async Task<ForcePlayerMoveAnswer> QueryPlayerForMove()
        {
            var newState = await Step();
            _chessBoardState = newState;
            return new ForcePlayerMoveAnswer {ChessBoardState = newState};
        }


        public async Task<ChessBoardState> Step()
        {
            var move = await AskForNextMove();

            if (await IsLegalMove(new IsLegalMoveQuestion(_chessBoardState, move)))
            {
                _chessBoardState = await UpdateChessBoardState(move);
            }
            return _chessBoardState;
        }

        private async Task<ChessBoardState> UpdateChessBoardState(ChessMove move)
        {
            var answer =  await _pieceMover.Ask<UpdateStateAnswer>(new UpdateStateQuestion(_chessBoardState, move));
            return answer.ChessBoardState;
        }

        private async Task<bool> IsLegalMove(IsLegalMoveQuestion question)
        {
            var answer = await _arbiter.Ask<IsLegalMoveAnswer>(question);
            return answer.IsLegal;
        }

        private async Task<ChessMove> AskForNextMove()
        {
            GetNextMoveAnswer answer;
            var getNextMoveQuestion = new GetNextMoveQuestion {ChessBoardState = _chessBoardState};
            if (_chessBoardState.NextToMove == Player.White)
            {
                answer = await GetNextMove(_whitePlayer, getNextMoveQuestion);
            }
            else
            {
                answer = await GetNextMove(_blackPlayer, getNextMoveQuestion);
            }
            return answer.ChessMove;
        }

        private Task<GetNextMoveAnswer> GetNextMove(IActorRef player, GetNextMoveQuestion question )
        {
            return player.Ask<GetNextMoveAnswer>(question);
        }

        private async void InitializeBoardState()
        {
            var newBoard = await GetNewBoard();
            _chessBoardState = newBoard.ChessBoardState;
        }

        private Task<CreateNewBoardAnswer> GetNewBoard()
        {
            return  _arbiter.Ask<CreateNewBoardAnswer>(new CreateNewBoardQuestion());
        }
    }
}