using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Actors._examples;
using ChezGeek.Common.Messages;
using Geek2k16.Entities.Enums;

namespace ChezGeek.Common.Actors
{
    public abstract class PlayerActor : ReceiveActor
    {
        private readonly IActorRef _randomMoveActor;

        protected PlayerActor()
        {
            _randomMoveActor = Context.ActorOf(Props.Create<RandomMoveActor>(Player.White));

            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetNextMove(question)));
        }

        protected virtual Task<GetNextMoveAnswer> GetNextMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            return _randomMoveActor.Ask<GetNextMoveAnswer>(getNextMoveQuestion);
        }
    }
}
