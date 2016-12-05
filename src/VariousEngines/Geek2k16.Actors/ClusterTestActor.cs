using Akka.Actor;
using System;

namespace Geek2k16.Actors
{
    public class ClusterTestActor : ReceiveActor
    {
        private readonly Guid _id = Guid.NewGuid();

        public ClusterTestActor()
        {
            Receive<ClusterTestMessage>(m => Console.WriteLine($"{_id} received: {m.Message}"));
            Receive<object>(message => Console.WriteLine("Receiver unknown message"));
        }

        protected override void PreStart()
        {
            Console.WriteLine("Starting ClusterTestActor");

            base.PreStart();
        }

        protected override void PostStop()
        {
            Console.WriteLine("ClusterTestActor stopped");

            base.PostStop();
        }
    }

    public class ClusterTestMessage
    {
        public ClusterTestMessage(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
