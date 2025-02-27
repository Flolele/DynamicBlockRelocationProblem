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
	public class BacktrackingPointStrategy : IDynamicUpdateStrategy
	{
		private readonly BetterStartingPointFinder _restartPointFinder;
		private SimulationConfig _config;

		private ICostCalculator costCalc;

		public BacktrackingPointStrategy(ICostCalculator costCalculator, SimulationConfig config)
		{
			_restartPointFinder = new BetterStartingPointFinder(costCalculator);
			_config = config;
			costCalc = costCalculator;
		}

		public BlockRelocationProblemState Recalculate(
			BlockRelocationProblemState currentState,
			BlockRelocationProblemState? previousState,
			Queue<Move> plannedMoves,
			EEventType eventType)
		{
			if (eventType == EEventType.NewBlockEvent && currentState.BlockYardManager.GetArrivalStackCount() > costCalc.IdealArrivalQueueSize) //fallback to standard recalculation
			{
				var standardStrategy = new StandardRecalculationStrategy(_config);
				return standardStrategy.Recalculate(currentState, previousState, plannedMoves, eventType);
			}

			var originalMoves = plannedMoves.ToList();
			var restartingPoint = _restartPointFinder.BacktrackingRestartPoint(currentState, originalMoves);

			BlockRelocationProblemState? best = null;

			while (best is null)
			{
				best = restartingPoint.BeamSearch(_config.BeamWidth,
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

				if (restartingPoint.AppliedMoves.Count == 0)
					break; //no solution could be found even with the current state -> abort

				if (best is null)
					restartingPoint.UndoLast(); //backtrack
			}

			if (best is null)
				throw new InvalidOperationException("No solution found");

			return best;
		}

	}


}
