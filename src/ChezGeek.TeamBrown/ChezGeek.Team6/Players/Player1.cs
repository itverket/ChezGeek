using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using ChezGeek.Common.Attributes;
using ChezGeek.TeamBrown.Messages;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;

namespace ChezGeek.TeamBrown.Players
{
#if DEBUG
    [ChessPlayer("Fembot01")]
#endif
    public class Player1 : Master
    {
        public Player1(Player player) : base(player)
        {

        }

        protected override async Task<EvaluatedChessMove> GetNextMove(ChessBoardState state, CancellationToken cancellationToken)
        {
            var rootState = new PreliminaryBoardState(state);

            var availableStateMovesGen1 = ChessCalculationService.GetCurrentAvailableStateMoves(rootState);
            var availableStateMovesGen1ToGen2 = ChessCalculationService.GetAvailableMovesWithNextPlyOfAvailableMoves(availableStateMovesGen1);
            var legalStateMovesGen1 = ChessCalculationService.FilterToLegalStateMoves(availableStateMovesGen1ToGen2, availableStateMovesGen1);

            if (legalStateMovesGen1.NextMoves.Count == 1)
            {
                var onlyMove = legalStateMovesGen1.NextMoves.Single();
                return new EvaluatedChessMove
                {
                    Move = onlyMove,
                    Score = ChessCalculationService.GetValueOfPieces(state)
                };
            }

            var legalStateMovesGen1ToGen2 = availableStateMovesGen1ToGen2
                .Where(x => legalStateMovesGen1.NextMoves.Contains(x.Key.NextMove)).ToDictionary(x => x.Key, x => x.Value);

            var evaluationMessages = legalStateMovesGen1ToGen2.Select(y => new EvaluateMove(y.Key, y.Value));

            var evaluationTasks = evaluationMessages.Select(message => _evaluationRouter.Ask<MoveEvaluated>(message));

            var evaluatedMoves = await Task.WhenAll(evaluationTasks).ConfigureAwait(false);
            var gen1ToGen2Evaluations = evaluatedMoves.ToDictionary(x => x.StateAndMove, x => x.MovePlan);

            var bestPlan = GetRandomItem(ChessCalculationService.GetBestPlans(legalStateMovesGen1, gen1ToGen2Evaluations));

            var bestMove = bestPlan.ChainedMoves.First();

            return new EvaluatedChessMove
            {
                Move = bestMove.NextMove,
                Score = bestPlan.EstimatedValue
            };
        }
    }
}
