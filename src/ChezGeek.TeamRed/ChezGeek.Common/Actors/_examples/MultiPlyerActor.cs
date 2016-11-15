using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Geek2k16.Service;
using Akka.Cluster.Routing;

namespace ChezGeek.Common.Actors._examples
{
    [ChessPlayer("MultiPlyer")]
    public class MultiPlyerActor : ReceiveActor
    {
        private const int NumberOfWorkerActors = 8;
        private const int NumberOfActorsPerNode = 2;
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;
        private readonly IActorRef _workerRouter;

        public MultiPlyerActor(Player player)
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));
            _workerRouter = Context.ActorOf(Props.Create<MultiPlyerWorkerActor>()
                .WithRouter(new RoundRobinPool(NumberOfActorsPerNode)));
            //_workerRouter = Context.ActorOf(Props.Create<MultiPlyerWorkerActor>()
            //    .WithRouter(new ClusterRouterPool(new RoundRobinPool(NumberOfWorkerActors),
            //        new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, true, "node"))));
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            var state = getNextMoveQuestion.ChessBoardState;

            var rootState = new PreliminaryBoardState(state);
            var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = _chessCalculationsService
                .GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = _chessCalculationsService
                .FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

            if (legalStateMovesGen1.NextMoves.Count == 1)
            {
                var onlyMove = legalStateMovesGen1.NextMoves.Single();
                return new GetNextMoveAnswer(onlyMove, _chessCalculationsService.GetValueOfPieces(state));
            }

            var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                .ToDictionary(x => x.Key, x => x.Value);

            var workerQuestions = legalStateMovesGen1ToGen2
                .Select(y => new MultiPlyerWorkerQuestion(y.Key, y.Value));

            var tasks = workerQuestions.Select(q => _workerRouter.Ask<MultiPlyerWorkerAnswer>(q));
            var answers = await Task.WhenAll(tasks).ConfigureAwait(false);

            var gen1ToGen2Evaluations = answers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var bestPlan = _chessCalculationsService
                .GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations)
                .GetRandomItem(_random);

            var bestMove = bestPlan.ChainedMoves.First();

            return new GetNextMoveAnswer(bestMove.NextMove, bestPlan.EstimatedValue);
        }
    }
}