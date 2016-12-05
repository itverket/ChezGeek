using System;
using Akka.Actor;
using System.Threading.Tasks;
using Geek2k16.Entities;

namespace Geek2k16.Actors
{
    public class BoardActor : ReceiveActor
    {
        private readonly IActorRef _arbiterActor;
        private readonly IActorRef _whitePlayerActor;
        private readonly IActorRef _blackPlayerActor;

        private ChessBoardState _chessBoardState;

        /*
         * 
         * 
         *  THE BEHAVIOR OF THIS ACTOR IS DEFINED IN CHEZGEEK
         *  
         *  THIS CLASS IS ONLY HERE TO ALLOW THE CREATION OF THE NEW GUI ACTOR
         * 
         * 
         */

        public BoardActor(Type whitePlayerActorType, Type blackPlayerActorType)
        {
            _arbiterActor = Context.ActorOf<ArbiterActor>();
            _whitePlayerActor = Context.ActorOf(Props.Create(whitePlayerActorType));
            _blackPlayerActor = Context.ActorOf(Props.Create(blackPlayerActorType));

            Init();
        }

        private void Init()
        {
            ReceiveAsync<GetInitialGameStateQuestion>(async _ =>
            {
                var initialChessBoardState = await GetInitialChessBoardState();

                _chessBoardState = initialChessBoardState;

                Sender.Tell(new GetInitialGameStateAnswer(initialChessBoardState));

                Become(Ready);
            });
        }

        private Task<ChessBoardState> GetInitialChessBoardState()
        {
            throw new NotImplementedException();
        }

        private void Ready()
        {
            ReceiveAsync<GetNextGameStateQuestion>(async _ => Sender.Tell(await GetNextGameState(), Self));
        }

        private Task<GetNextGameStateAnswer> GetNextGameState()
        {
            throw new NotImplementedException();
        }
    }
}
