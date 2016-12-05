using System.Threading.Tasks;
using Akka.Actor;

namespace Geek2k16.Actors
{
    public abstract class PlayerActor : ReceiveActor
    {
        private readonly IActorRef _randomMove;

        protected PlayerActor()
        {
            _randomMove = Context.ActorOf<RandomMoveActor>();
            
            ReceiveAsync<GetNextMoveQuestion>(async q => Sender.Tell(await ResolveNextMove(q), Self));
        }

        protected Task<GetNextMoveAnswer> ResolveNextMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            return _randomMove.Ask<GetNextMoveAnswer>(getNextMoveQuestion);
        }
    }

    public class TestPlayerActor : PlayerActor
    {
        
    }
}