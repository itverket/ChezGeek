using Akka.Actor;

namespace Geek2k16.Actors
{
    public class DllListenerActor : ReceiveActor
    {
        public DllListenerActor()
        {
            //Listen for DLL's with public Actor-classes which inherit from PlayerActor
        }
    }
}