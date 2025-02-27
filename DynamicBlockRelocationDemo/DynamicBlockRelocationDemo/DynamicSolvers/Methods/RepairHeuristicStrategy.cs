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

namespace DynamicBlockRelocationDemo.DynamicSolvers.Methods
{
	public class RepairHeuristicStrategy : IDynamicUpdateStrategy
	{
		private readonly ICostCalculator _costCalculator;
		private SimulationConfig _config;

		public RepairHeuristicStrategy(ICostCalculator costCalculator, SimulationConfig config)
		{
			_costCalculator = costCalculator;
			_config = config;
		}

		public BlockRelocationProblemState Recalculate(
			BlockRelocationProblemState currentState,
			BlockRelocationProblemState? previousState,
			Queue<Move> plannedMoves,
			EEventType eventType)
		{
			RepairHeuristicSolutionSolver insertionHeursitcSolver = new RepairHeuristicSolutionSolver(_costCalculator);
			var best = insertionHeursitcSolver.ApplyRepairHeuristic(currentState, plannedMoves);

			if (best is null)
			{
				// Fallback to standard recalculation if too expensive
				var standardStrategy = new StandardRecalculationStrategy(_config);
				best = standardStrategy.Recalculate(currentState, previousState, plannedMoves, eventType);
			}

			return best;
		}
	}

}
