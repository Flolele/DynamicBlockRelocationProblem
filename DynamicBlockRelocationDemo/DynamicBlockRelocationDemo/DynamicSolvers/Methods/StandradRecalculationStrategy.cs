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
	public class StandardRecalculationStrategy : IDynamicUpdateStrategy
	{
		private SimulationConfig _config;
		public StandardRecalculationStrategy(SimulationConfig config)
		{
			_config = config;
		}
		public BlockRelocationProblemState Recalculate(
			BlockRelocationProblemState currentState,
			BlockRelocationProblemState? previousState,
			Queue<Move> plannedMoves,
			EEventType eventType)
		{
			var bestState = currentState.BeamSearch(_config.BeamWidth,
				state =>
				{
					var boundValue = state.Bound.Value;
					var blockedBlocks = state.AppliedMoves.Any()
						? state.BlockYardManager.GetBlockedBlocks()
						: 0;
					return boundValue + blockedBlocks * 10; //penalize for blocked blocks
				},
				runtime: _config.DynamicUpdateTimeout
			);

			if (bestState is null)
				throw new InvalidOperationException("No solution found");

			return bestState;
		}
	}
}
