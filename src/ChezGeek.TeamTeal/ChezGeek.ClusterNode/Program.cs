using Akka.Actor;
using System;
using System.Threading;

namespace ChezGeek.ClusterNode
{
    class Program
    {
        private static readonly ManualResetEvent _terminatedEvent = new ManualResetEvent(false);
        private static readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.Title = "ChezGeek.ClusterNode (Please use Ctrl + C to shutdown)";

            Console.CancelKeyPress += Console_CancelKeyPress;

            var chessCluster = ActorSystem.Create("ChezCluster");

            _exitEvent.WaitOne();

            var cluster = Akka.Cluster.Cluster.Get(chessCluster);
            cluster.RegisterOnMemberRemoved(() => MemberRemoved(chessCluster));
            cluster.Leave(cluster.SelfAddress);

            _terminatedEvent.WaitOne();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _exitEvent.Set();
            e.Cancel = true;
        }

        private static async void MemberRemoved(ActorSystem actorSystem)
        {
            await actorSystem.Terminate();

            _terminatedEvent.Set();
        }
    }
}
