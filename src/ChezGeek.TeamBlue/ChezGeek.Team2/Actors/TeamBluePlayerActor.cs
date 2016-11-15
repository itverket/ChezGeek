using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Routing;
using ChezGeek.Common.Attributes;
using ChezGeek.Common.Messages;
using ChezGeek.Team2;
using ChezGeek.TeamBlue.Services;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using MultiPlyerPlusWorkerAnswer = ChezGeek.Team2.Messages.MultiPlyerPlusWorkerAnswer;
using MultiPlyerPlusWorkerQuestion = ChezGeek.Team2.Messages.MultiPlyerPlusWorkerQuestion;

namespace ChezGeek.TeamBlue.Actors
{
    [ChessPlayer("DotNetflix&Kiel")]
    public class TeamBluePlayerActor : ReceiveActor
    {
        private const int NumberOfWorkerActors = 50;
        private const int NumberOfActorsPerNode = 5;
        private const int DivideRemainingTimeBy = 10;
        private readonly ChessCalculationsService _chessCalculationsService;
        private readonly Random _random;
        private readonly IActorRef _workerRouter;
        private int _timeout = 15;
        private MultiPlyerPlusWorkerAnswer[] _currentAnwsers;
        private double _remainingTimeInSeconds;

        public TeamBluePlayerActor(Player player)
        {
            _random = new Random();
            _chessCalculationsService = new ChessCalculationsService();
            ReceiveAsync<GetNextMoveQuestion>(async question => Sender.Tell(await GetBestMove(question), Self));
            //_workerRouter = Context.ActorOf(Props.Create<MultiPlyerPlusWorkerActor>()
            //     .WithRouter(new RoundRobinPool(NumberOfActorsPerNode)));
            _workerRouter = Context.ActorOf(Props.Create<MultiPlyerPlusWorkerActor>()
                .WithRouter(new ClusterRouterPool(new RoundRobinPool(NumberOfWorkerActors),
                    new ClusterRouterPoolSettings(NumberOfWorkerActors, NumberOfActorsPerNode, false, "node"))));
        }

        private async Task<GetNextMoveAnswer> GetBestMove(GetNextMoveQuestion getNextMoveQuestion)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                var state = getNextMoveQuestion.ChessBoardState;
                var remainingTime = state.NextToMove == Player.Black ? state.BlackTime : state.WhiteTime;
                _timeout = (int) (remainingTime.TotalSeconds/ (DivideRemainingTimeBy+1));
                var timeLimitSeconds = _remainingTimeInSeconds / DivideRemainingTimeBy;

                var rootState = new PreliminaryBoardState(state);

                
                // LEGAL MOVES

                var availableStateMovesGen1 = _chessCalculationsService.GetCurrentAvailableStateMoves(rootState);
                var availableStateMovesGen1ToGen2 = _chessCalculationsService
                    .GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);

                if (state.MoveHistory.Count < 2)
                {
                    var player = state.NextToMove;

                    ChessPosition position = new ChessPosition();
                    var chessPiecePosition = new ChessPiecePosition();
                    if (player == Player.White)
                    {
                        position = new ChessPosition(ChessColumn.E, ChessRow.Row4);
                        chessPiecePosition = new ChessPiecePosition(player, PieceType.Pawn, ChessColumn.E, ChessRow.Row2);

                    }
                    else
                    {
                         return new GetNextMoveAnswer(availableStateMovesGen1.NextMoves.ToArray()[1],0);

                    }
                    return new GetNextMoveAnswer(new ChessMove(chessPiecePosition, position), 0);
                }

                AvailableStateMoves legalStateMovesGen1 = _chessCalculationsService
                    .FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);
               var firstGenMove = Extensions.GetRandomItem(legalStateMovesGen1.NextMoves.ToArray(),_random);
                var answer = new GetNextMoveAnswer(firstGenMove,0);

                // 2 PLY

                var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                    .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove))
                    .ToDictionary(x => x.Key, x => x.Value);

                var firstGen2Evaluations = legalStateMovesGen1ToGen2.ToDictionary(x => x.Key,
                    x => Extensions.GetRandomItem(_chessCalculationsService.GetBestPlans(x.Value), _random));

                answer = ReturnBestMove(legalStateMovesGen1, firstGen2Evaluations);
                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds)
                {
                    return answer;
                }

                // 3 PLY

                var workerQuestions = legalStateMovesGen1ToGen2
                    .Select(y => new MultiPlyerPlusWorkerQuestion(y.Key, y.Value));
                IEnumerable<Task<MultiPlyerPlusWorkerAnswer>> tasks;
                MultiPlyerPlusWorkerAnswer[] answers = null;
                try
                {
                    tasks =
                        workerQuestions.Select(
                            q => _workerRouter.Ask<MultiPlyerPlusWorkerAnswer>(q, TimeSpan.FromSeconds(_timeout)));
                    answers = await Task.WhenAll(tasks).ConfigureAwait(false);

                    ValidateExceptions(answers);

                    var secondGen2Evaluations = answers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

                    answer = ReturnBestMove(legalStateMovesGen1, secondGen2Evaluations);
                }
                catch (Exception e)
                {
                    _workerRouter.Tell(new Broadcast(new Cancel()));
                    return answer;
                }

                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds)
                {
                    return answer;
                }
                // 4 PLY
                Dictionary<PreliminaryStateAndMove, MovePlan> thirdGen2Evaluations;
                try
                {
                    thirdGen2Evaluations = (await Task.WhenAll(answers.Select(AggregateSecondGradeAnswers)))
                        .ToDictionary(x => x.Key, x => x.Value);
                }
                catch (Exception e)
                {
                    _workerRouter.Tell(new Broadcast(new Cancel()));
                    return answer;
                }
                answer = ReturnBestMove(legalStateMovesGen1, thirdGen2Evaluations);

                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds)
                {
                    return answer;
                }
                //5 PLY
                Dictionary<PreliminaryStateAndMove, MovePlan> fourthGen2Evaluations;
                try
                {
                    fourthGen2Evaluations = (await Task.WhenAll(_currentAnwsers.Select(AggregateSecondGradeAnswers)))
                        .ToDictionary(x => x.Key, x => x.Value);
                }
                catch (Exception e)
                {
                    _workerRouter.Tell(new Broadcast(new Cancel()));
                    return answer;
                }
                answer = ReturnBestMove(legalStateMovesGen1, fourthGen2Evaluations);
                if (stopwatch.Elapsed.TotalSeconds > timeLimitSeconds)
                {
                    return answer;
                }
                //6 PLY
                Dictionary<PreliminaryStateAndMove, MovePlan> fifthGen2Evaluations;
                try
                {
                    fifthGen2Evaluations = (await Task.WhenAll(_currentAnwsers.Select(AggregateSecondGradeAnswers)))
                        .ToDictionary(x => x.Key, x => x.Value);
                }
                catch (Exception e)
                {
                    _workerRouter.Tell(new Broadcast(new Cancel()));
                    return answer;
                }
                return ReturnBestMove(legalStateMovesGen1, fifthGen2Evaluations);
            }
            catch (Exception exception)
            {
                return new GetNextMoveAnswer(exception.Message);
            }
        }

        private async Task<KeyValuePair<PreliminaryStateAndMove, MovePlan>> AggregateSecondGradeAnswers(MultiPlyerPlusWorkerAnswer answer)
        {
            var newQuestions =
                answer.FromPreviousPlyToThisPly.Select(y => new MultiPlyerPlusWorkerQuestion(y.Key, y.Value, true));
            var newTasks = newQuestions.Select(q => _workerRouter.Ask<MultiPlyerPlusWorkerAnswer>(q, TimeSpan.FromSeconds(_timeout)));
            var awnsers = await Task.WhenAll(newTasks).ConfigureAwait(false);

            ValidateExceptions(awnsers);

            var firstGen3Evaluations = awnsers.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var multiPlyerPlusWorkerAnswers = (MultiPlyerPlusWorkerAnswer[]) awnsers.Clone();
            _currentAnwsers = multiPlyerPlusWorkerAnswers;

            var newPlan = Extensions.GetRandomItem(_chessCalculationsService
                    .GetBestPlans(answer.AvailableStateMoves, firstGen3Evaluations), _random);

            return new KeyValuePair<PreliminaryStateAndMove, MovePlan>(answer.StateAndMove, newPlan);
        }

        private GetNextMoveAnswer ReturnBestMove(AvailableStateMoves legalStateMovesGen1,
            Dictionary<PreliminaryStateAndMove, MovePlan> firstGen2Evaluations)
        {
            var bestPlan = Extensions.GetRandomItem(_chessCalculationsService
                    .GetBestPlans(legalStateMovesGen1, firstGen2Evaluations), _random);
            var bestMove = bestPlan.ChainedMoves.First();
            return new GetNextMoveAnswer(bestMove.NextMove, bestPlan.EstimatedValue);
        }

        private static void ValidateExceptions(MultiPlyerPlusWorkerAnswer[] answers)
        {
            var exception = answers.FirstOrDefault(x => x.ExceptionMessage != null)?.ExceptionMessage;
            if (exception != null) throw new Exception(exception);
        }

    }
}