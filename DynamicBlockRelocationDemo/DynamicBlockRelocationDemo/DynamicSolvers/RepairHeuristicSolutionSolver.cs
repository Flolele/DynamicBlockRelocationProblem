using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreesearchLib;

namespace DynamicBlockRelocationDemo.BlockRelocation.DynamicImprovements
{
    public class RepairHeuristicSolutionSolver
    {
        private readonly ICostCalculator _costCalculator;
        private const float BASE_RECALCULATION_THRESHOLD = 1.5f; //if new solution costs more than 1.5x original, trigger recalculation
        private const float MAX_THRESHOLD = 2.5f; //maximum allowable threshold
        private const int MOVE_COUNT_THRESHOLD = 3; //number of moves below which we start being more lenient



        public RepairHeuristicSolutionSolver(ICostCalculator costCalculator)
        {
            _costCalculator = costCalculator;
        }

        public BlockRelocationProblemState? ApplyRepairHeuristic(BlockRelocationProblemState currentState, Queue<Move> plannedMoves)
        {
			var workingState = (BlockRelocationProblemState)currentState.Clone();
			var originalMoves = new List<Move>(plannedMoves);
			var newSolution = new List<Move>();
			float originalCost = _costCalculator.GetTotalMoveCost(originalMoves);

			// Update all moves with current stack heights
			var updatedMoves = UpdateMovesWithCurrentStackHeights(originalMoves, workingState);
			var (validMoves, invalidMoves) = CategorizeMoves(workingState, updatedMoves);

			// Apply the updated moves
			foreach (var move in updatedMoves)
			{
				workingState.Apply(move);
				newSolution.Add(move);
			}

			// for each invalid move, try to create a sequence of moves to achieve similar results
			while (invalidMoves.Any())
            {
                var invalidMove = invalidMoves.First();
                var correctiveMoves = HandleInvalidMove(workingState, invalidMove, invalidMoves);
                if (correctiveMoves != null)
                {
                    foreach (var move in correctiveMoves)
                    {
                        workingState.Apply(move);
                        newSolution.Add(move);
                    }
                }
                invalidMoves.Remove(invalidMove);
            }

            //check if we still need additional moves to reach target positions
            var completionMoves = GenerateCompletionMoves(workingState);
            if (completionMoves != null)
            {
                foreach (var move in completionMoves)
                {
                    workingState.Apply(move);
                    newSolution.Add(move);
                }
            }

            //check if solution cost is acceptable
            float newCost = _costCalculator.GetTotalMoveCost(newSolution);
            if (newCost > originalCost + 300)
            {
                return null; //signal need for full recalculation
            }
            if (!workingState.IsTerminal)
                throw new InvalidOperationException("Insertion Heurstic created wrong solution");

            return workingState;
        }

        private IEnumerable<Move> GenerateCompletionMoves(BlockRelocationProblemState workingState)
        {
            if(workingState.IsTerminal)
            {
                return Enumerable.Empty<Move>();
            }
            var tempState = (BlockRelocationProblemState)workingState.Clone();
            tempState.Reset();
            var bestState = tempState.BeamSearch(2,
				state =>
				{
					var boundValue = state.Bound.Value;
					var blockedBlocks = state.AppliedMoves.Any()
						? state.BlockYardManager.GetBlockedBlocks()
						: 0;
					return boundValue + (blockedBlocks * 10); //penalize for blocked blocks
				},
				runtime: TimeSpan.FromSeconds(1000)
			);
			if (bestState is null)
            {
                return null;
            }
            return bestState.AppliedMoves.Reverse();
        }

        private IEnumerable<Move> HandleInvalidMove(BlockRelocationProblemState workingState, Move invalidMove, IEnumerable<Move> remainingOriginalMoves)
        {
            var block = workingState.BlockYardManager.GetBlock(invalidMove.BlockId);
            if (block == null) 
                return null;

            var currentPos = workingState.BlockYardManager.GetPosition(block);
            var correctiveMoves = new List<Move>();

            if (currentPos == BlockArea.VOID_POSITION)
                return correctiveMoves; //move is not needed anymore

            //if block is not at top, we need to move blocks above it
            Position cranePositionAfterClearingMoves = workingState.BlockYardManager.GetPositionOfCrane(1);
            if (!workingState.BlockYardManager.IsBlockAccessible(block))
            {
                var clearingMoves = ClearBlocksAbove(workingState, block, remainingOriginalMoves);
                if (clearingMoves == null) 
                    return null;
                correctiveMoves.AddRange(clearingMoves);
                cranePositionAfterClearingMoves = correctiveMoves.Last().CraneTargetPosition;
            }

            //Now try to move the block to its target position
            if (invalidMove.TargetPosition != null)
            {
                //If target position is occupied or blocked, find temporary position
                if (!workingState.BlockYardManager.GetFreePositions().Contains(invalidMove.TargetPosition))
                {
                    var tempPos = FindBestTemporaryPosition(workingState, block, invalidMove.TargetPosition, remainingOriginalMoves);
                    if (tempPos == null) 
                        return null;

                    var move = new Move(1, block.Id, cranePositionAfterClearingMoves, invalidMove.TargetPosition, workingState.BlockYardManager.GetPosition(block), tempPos, workingState.BlockYardManager.GetArrivalStackCount());
                    correctiveMoves.Add(move);
                }
                else
                {
                    //crane position must be wrong, so update it to new one
                    var move = new Move(1, block.Id, cranePositionAfterClearingMoves, invalidMove.TargetPosition, workingState.BlockYardManager.GetPosition(block), invalidMove.TargetPosition, workingState.BlockYardManager.GetArrivalStackCount());
                    correctiveMoves.Add(move);
                }
            }

            return correctiveMoves;
        }

        private (List<Move> validMoves, List<Move> invalidMoves) CategorizeMoves(BlockRelocationProblemState state, List<Move> moves)
        {
            var validMoves = new List<Move>();
            var invalidMoves = new List<Move>();

            var stateClone = (BlockRelocationProblemState)state.Clone();

            bool foundInvalid = false;
            foreach (var move in moves)
            {
                var newMove = new Move(move.CraneId, move.BlockId, move.CraneSourcePosition, move.CraneTargetPosition, move.BlockSourcePosition, move.TargetPosition, state.BlockYardManager.GetArrivalStackCount());
				if (!foundInvalid && TryApplyMove(stateClone, newMove))
                {
                    validMoves.Add(move);
                }
                else
                {
                    foundInvalid = true;
                    invalidMoves.Add(move);
                }
            }

            return (validMoves, invalidMoves);
        }

        private bool TryApplyMove(BlockRelocationProblemState state, Move move)
        {
            try
            {
                state.Apply(move);
                return true;
            }
            catch(InvalidOperationException e)
            {
                return false;
            }
        }

		private List<Move> UpdateMovesWithCurrentStackHeights(List<Move> moves, BlockRelocationProblemState state)
		{
			var updatedMoves = new List<Move>();
			var workingState = (BlockRelocationProblemState)state.Clone();

			foreach (var move in moves)
			{
				// Create new move with current arrival stack height
				var updatedMove = new Move(
					move.CraneId,
					move.BlockId,
					move.CraneSourcePosition,
					move.CraneTargetPosition,
					move.BlockSourcePosition,
					move.TargetPosition,
					workingState.BlockYardManager.GetArrivalStackCount()
				);

				if (TryApplyMove(workingState, updatedMove))
				{
					updatedMoves.Add(updatedMove);
				}
				else
				{
					break;
				}
			}

			return updatedMoves;
		}

		private IEnumerable<Move> ClearBlocksAbove(BlockRelocationProblemState originalState, Block block, IEnumerable<Move> remainingMoves)
        {
            BlockRelocationProblemState state = (BlockRelocationProblemState)originalState.Clone();
            //try clearing all the blocks above the source position to make it accessible by moving the blocks to temporary/intermediate positions that have a low cost/interference factor
            var clearingMoves = new List<Move>();
            var topBlocks = state.BlockYardManager.GetBlocksAbove(block);

            while (topBlocks.Any())
            {
                Block blockToMove = topBlocks.First(); //TODO maybe check if there is a move for the block in a later stage of the applied moves
                blockToMove.TryGetTargetPosition(out Position? eventualTarget);
                var tempPos = FindBestTemporaryPosition(state, blockToMove, eventualTarget, remainingMoves);
                if (tempPos == null) 
                    return null;

                var move = new Move(1, blockToMove.Id, state.BlockYardManager.GetPositionOfCrane(1), tempPos, state.BlockYardManager.GetPosition(blockToMove), tempPos, state.BlockYardManager.GetArrivalStackCount());

                clearingMoves.Add(move);
                state.Apply(move);
                //update blocks above
                topBlocks = state.BlockYardManager.GetBlocksAbove(block);
            }

            return clearingMoves;
        }
        private Position? FindBestTemporaryPosition(BlockRelocationProblemState state, Block block, Position eventualTarget, IEnumerable<Move> remainingMoves)
        {
            //can you help me to incoparte interference score aswell as the distance from the target position
            var currentPos = state.BlockYardManager.GetPosition(block);
            var candidates = state.BlockYardManager.GetFreePositions()
                .Where(pos => !(pos.X == currentPos.X && pos.Z == currentPos.Z)) //remove positions in the same stack
                .OrderBy(pos => EstimatePositionInterference(state, pos, remainingMoves));

            if (eventualTarget != null)
            {
                candidates = candidates.OrderBy(pos =>
                    Math.Abs(pos.X - eventualTarget.X) +
                    Math.Abs(pos.Z - eventualTarget.Z));
            }
            else
            {
                //otherwise prefer positions that are less likely to block future moves
                candidates = candidates.OrderBy(pos =>
                    EstimatePositionInterference(state, pos, remainingMoves));
            }

            return candidates.FirstOrDefault();
        }

        private int EstimatePositionInterference(BlockRelocationProblemState state, Position position, IEnumerable<Move> remainingMoves)
        {
            if(position == BlockArea.VOID_POSITION)
            {
                return 0;
            }
            //count how many future moves might be affected by using this position
            int interference = 0;

            //check proximity to future move sources and targets
            foreach (var move in remainingMoves)
            {                if (move.BlockSourcePosition.X == position.X &&
                    move.BlockSourcePosition.Z == position.Z)
                    interference++;

                if (move.TargetPosition.X == position.X &&
                    move.TargetPosition.Z == position.Z)
                    interference++;
            }
            return interference;
        }
        private float CalculateDynamicThreshold(int remainingMoves)
        {
            if (remainingMoves >= MOVE_COUNT_THRESHOLD)
                return BASE_RECALCULATION_THRESHOLD;

            // As remainingMoves approaches 0, threshold approaches MAX_THRESHOLD
            float progressFactor = (float)(MOVE_COUNT_THRESHOLD - remainingMoves) / MOVE_COUNT_THRESHOLD;
            return BASE_RECALCULATION_THRESHOLD + (MAX_THRESHOLD - BASE_RECALCULATION_THRESHOLD) * progressFactor;
        }
    }
}
