using Akka.Actor;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Geek2k16.Cluster
{
    class Program
    {
        private static readonly ManualResetEvent _terminatedEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var chessCluster = ActorSystem.Create("ChessCluster");

            Thread.Sleep(3000);

            Console.WriteLine("ChessCluster running");
            Console.WriteLine();
            Console.WriteLine("Press a key to terminate cluster...");

            Console.ReadKey();

            var cluster = Akka.Cluster.Cluster.Get(chessCluster);
            cluster.RegisterOnMemberRemoved(() => MemberRemoved(chessCluster));
            cluster.Leave(cluster.SelfAddress);

            _terminatedEvent.WaitOne();

            Console.WriteLine("ChessCluster stopped");
            Console.WriteLine();
            Console.WriteLine("Press a key to exit...");

            Console.ReadKey();
        }

        private static async void MemberRemoved(ActorSystem actorSystem)
        {
            await actorSystem.Terminate();

            _terminatedEvent.Set();
        }
    }
}
