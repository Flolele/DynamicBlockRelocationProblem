using DynamicBlockRelocationDemo.BlockRelocation.DynamicImprovements;
using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreesearchLib;

namespace DynamicBlockRelocationDemo.DynamicSolvers.Methods
{
	public class AdaptiveRestartPointStrategy : IDynamicUpdateStrategy
	{
		private readonly BetterStartingPointFinder _restartPointFinder;
		private SimulationConfig _config;

		public AdaptiveRestartPointStrategy(ICostCalculator costCalculator, SimulationConfig config)
		{
			_restartPointFinder = new BetterStartingPointFinder(costCalculator);
			_config = config;
		}

		public BlockRelocationProblemState Recalculate(
			BlockRelocationProblemState currentState,
			BlockRelocationProblemState? previousState,
			Queue<Move> plannedMoves,
			EEventType eventType)
		{
			if (previousState is null)
				throw new InvalidOperationException("No previous state to compare with");

			var originalMoves = plannedMoves.ToList();
			var restartingPoint = _restartPointFinder.FindOptimizedSolution(previousState, currentState, plannedMoves);

			var moves = restartingPoint.AppliedMoves; //assert that all moves are contained

			var newSolution = restartingPoint.BeamSearch(_config.BeamWidth,
				state =>
				{
					var boundValue = state.Bound.Value;
					var blockedBlocks = state.AppliedMoves.Any()
						? state.BlockYardManager.GetBlockedBlocks()
						: 0;
					return boundValue + blockedBlocks * 10;
				},
				runtime: _config.DynamicUpdateTimeout
			);

			//assert that all moves are contained
			foreach (var move in moves)
			{
				if (!newSolution.AppliedMoves.Contains(move))
					throw new InvalidOperationException("Not all moves are contained in the new solution");
			}

			if (newSolution is null)
				throw new InvalidOperationException("No solution found from restart point");

			return newSolution;
		}
	}

}
