using System;
using Akka.Actor;
using Geek2k16.Actors;
using Akka.Routing;
using System.Threading;
using System.Threading.Tasks;

namespace Geek2k16.AkkaTest
{
    class Program
    {
        class TestActor : ReceiveActor
        {
            private readonly IActorRef _router;

            public TestActor()
            {
                _router = Context.ActorOf(Props.Create(() => new ClusterTestActor()).WithRouter(FromConfig.Instance), "clusterTest");

                Receive<StartSpammingMessage>(message =>
                {
                    SpamMessages();

                    return true;
                });
            }

            private void SpamMessages()
            {
                _router.Tell(new ClusterTestMessage("1"));
                _router.Tell(new ClusterTestMessage("2"));
                _router.Tell(new ClusterTestMessage("3"));
                _router.Tell(new ClusterTestMessage("4"));
                _router.Tell(new ClusterTestMessage("5"));
                _router.Tell(new ClusterTestMessage("6"));
                _router.Tell(new ClusterTestMessage("7"));
                _router.Tell(new ClusterTestMessage("8"));
                _router.Tell(new ClusterTestMessage("9"));
                _router.Tell(new ClusterTestMessage("10"));
                _router.Tell(new ClusterTestMessage("11"));
                _router.Tell(new ClusterTestMessage("12"));
                _router.Tell(new ClusterTestMessage("13"));
                _router.Tell(new ClusterTestMessage("14"));
                _router.Tell(new ClusterTestMessage("15"));
                _router.Tell(new ClusterTestMessage("16"));
                _router.Tell(new ClusterTestMessage("17"));
                _router.Tell(new ClusterTestMessage("18"));
                _router.Tell(new ClusterTestMessage("19"));
                _router.Tell(new ClusterTestMessage("20"));
            }

            protected override void PreStart()
            {
                Console.WriteLine("Starting TestActor");

                base.PreStart();
            }

            protected override void PostStop()
            {
                Console.WriteLine("TestActor stopped");

                base.PostStop();
            }
        }

        class StartSpammingMessage { }

        private static readonly ManualResetEvent _terminatedEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var chessCluster = ActorSystem.Create("ChessCluster");

            Thread.Sleep(3000);

            var actor = chessCluster.ActorOf<TestActor>("test");

            Thread.Sleep(3000);

            Console.WriteLine("Press a key to start spamming...");
            Console.ReadKey();

            actor.Tell(new StartSpammingMessage());

            Thread.Sleep(3000);

            Console.WriteLine("Press a key to kill actors...");
            Console.ReadKey();

            actor.Tell(PoisonPill.Instance);

            Thread.Sleep(3000);

            Console.WriteLine("Press a key to terminate system...");
            Console.ReadKey();

            var cluster = Akka.Cluster.Cluster.Get(chessCluster);
            cluster.RegisterOnMemberRemoved(() => MemberRemoved(chessCluster));
            cluster.Leave(cluster.SelfAddress);

            _terminatedEvent.WaitOne();

            Console.WriteLine("Press a key to exit");
            Console.ReadKey();
        }

        private static async void MemberRemoved(ActorSystem actorSystem)
        {
            await actorSystem.Terminate();

            _terminatedEvent.Set();
        }
    }
}
