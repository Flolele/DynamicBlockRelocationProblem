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
	public class BetterStartingPointFinder
	{
		private readonly ICostCalculator _costCalculator;

		public BetterStartingPointFinder(ICostCalculator costCalculator)
		{
			_costCalculator = costCalculator;
		}

		public record struct CostComparisonPoint(
			float OriginalCost,
			float CurrentCost,
			float BoundDifference
		);

		public BlockRelocationProblemState FindOptimizedSolution(
			BlockRelocationProblemState previousState,
			BlockRelocationProblemState currentState,
			Queue<Move> plannedMoves)
		{
			var list = plannedMoves.ToList();
			var costComparison = AnalyzeCostDifferences(previousState, currentState, list);

			if (!costComparison.Any())
			{
				return currentState;
			}

			// Find optimal restart point based on cost differences
			int restartIndex = DetermineRestartPoint(costComparison);

			// Apply moves up to restart point
			var startingState = (BlockRelocationProblemState)currentState.Clone();
			for (int i = 0; i < restartIndex; i++)
			{
				startingState.Apply(costComparison[i].Move);
			}

			return startingState;
		}

		private List<(Move Move, CostComparisonPoint Comparison)> AnalyzeCostDifferences(
			BlockRelocationProblemState previousState,
			BlockRelocationProblemState currentState,
			List<Move> plannedMoves)
		{
			var result = new List<(Move Move, CostComparisonPoint Comparison)>();

			// Create working copies of both states
			var previousWorkingState = (BlockRelocationProblemState)previousState.Clone();
			previousWorkingState.Reset();
			var currentWorkingState = (BlockRelocationProblemState)currentState.Clone();

			// Update moves with current arrival stack heights
			var updatedMoves = UpdateMovesWithCurrentStackHeights(plannedMoves, currentState);

			for (int i = 0; i < updatedMoves.Count; i++)
			{
				var orignalMove = plannedMoves[i];
				var move = updatedMoves[i];

				// Try to apply moves
				bool previousValid = TryApplyMove(previousWorkingState, orignalMove);
				bool currentValid = TryApplyMove(currentWorkingState, move);


				if (!currentValid || !previousValid) // If move is not valid in current state, stop
					break;
				float previousBound = previousWorkingState.Bound.Value + previousWorkingState.BlockYardManager.GetBlockedBlocks() * 10;
				float currentBound = currentWorkingState.Bound.Value + currentWorkingState.BlockYardManager.GetBlockedBlocks() * 10;

				float previousAccumulatedCost = _costCalculator.GetTotalMoveCost(previousWorkingState.AppliedMoves.Reverse());
				float currentAccumulatedCost = _costCalculator.GetTotalMoveCost(currentWorkingState.AppliedMoves.Reverse());


				float boundDifference = currentBound - previousBound;

				var comparisonPoint = new CostComparisonPoint(
					OriginalCost: previousAccumulatedCost,
					CurrentCost: currentAccumulatedCost,
					BoundDifference: boundDifference
				);

				result.Add((move, comparisonPoint));
			}

			return result;
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

		public BlockRelocationProblemState BacktrackingRestartPoint(BlockRelocationProblemState state, List<Move> plannedMoves)
		{
			//try applying the current moves
			var workingState = (BlockRelocationProblemState)state.Clone();
			foreach (var move in plannedMoves)
			{
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
					//do nothing
				}
				else
				{
					break;
				}
			}

			return workingState;
		}

		private bool TryApplyMove(BlockRelocationProblemState state, Move move)
		{
			try
			{
				state.Apply(move);
				return true;
			}
			catch (Exception)
			{
				if(state.AppliedMoves.Contains(move))
				{
					throw new InvalidOperationException("Move was not undone");
				}
				return false;
			}
		}

		private int DetermineRestartPoint(List<(Move Move, CostComparisonPoint Comparison)> costComparison)
		{
			if (!costComparison.Any())
				return 0;

			const float BETA = 30f;

			// Find first point where cost difference exceeds beta
			for (int i = 0; i < costComparison.Count - 1; i++)
			{
				var boundDiff = costComparison[i + 1].Comparison.BoundDifference;
				var costDiff =  costComparison[i + 1].Comparison.CurrentCost - costComparison[i + 1].Comparison.OriginalCost;
				if (boundDiff >= (BETA + 10) || costDiff >= BETA)
				{
					return i;  // Return one point before where difference exceeds beta
				}
			}

			// If no point exceeds beta, return k (total number of valid moves)
			return costComparison.Count;
			//if (!costComparison.Any())
			//	return 0;

			//const float SIGNIFICANT_COST_INCREASE = 50f;  // Adjust this value based on testing
			//float worstSeenDiff = 0;
			//int worstPoint = -1;

			//// Find the move with the biggest cost increase
			//for (int i = 0; i < costComparison.Count - 1; i++)
			//{
			//	var costDiff = costComparison[i + 1].Comparison.CostDifference;

			//	if (costDiff > worstSeenDiff)
			//	{
			//		worstSeenDiff = costDiff;
			//		worstPoint = i;
			//	}
			//}

			//// If we found a significant increase, restart at that point
			//if (worstSeenDiff > SIGNIFICANT_COST_INCREASE)
			//{
			//	return worstPoint;
			//}

			//// If no significant increases, keep the whole solution
			//return costComparison.Count;
		}
	}

	//private BlockRelocationProblemState? RunFullRecalculation(BlockRelocationProblemState state)
	//{
	//    return state.BeamSearch(
	//        beamWidth: 2,
	//        rank: nextState => nextState.Bound.Value + (nextState.BlockYardManager.GetBlockedBlocks() * 10),
	//        runtime: TimeSpan.FromSeconds(1000)
	//    );
	//}

	//private BlockRelocationProblemState? RunPartialRecalculation(BlockRelocationProblemState state)
	//{
	//    return state.BeamSearch(
	//        beamWidth: 3,
	//        rank: nextState =>
	//        {
	//            var boundValue = nextState.Bound.Value;
	//            var blockedBlocks = nextState.BlockYardManager.GetBlockedBlocks();
	//            return boundValue + (blockedBlocks * 10);
	//        },
	//        runtime: TimeSpan.FromSeconds(1000)
	//    );
	//}
}
