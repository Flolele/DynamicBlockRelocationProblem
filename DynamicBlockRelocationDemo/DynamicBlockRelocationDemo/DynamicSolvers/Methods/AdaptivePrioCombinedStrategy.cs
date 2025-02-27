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
	public class PrioritizeWithAdaptiveRestartPointStrategy : IDynamicUpdateStrategy
	{
		private readonly BetterStartingPointFinder _restartPointFinder;
		private SimulationConfig _config;

		public PrioritizeWithAdaptiveRestartPointStrategy(ICostCalculator costCalculator, SimulationConfig config)
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



			var restartingPoint = _restartPointFinder.FindOptimizedSolution(previousState, currentState, plannedMoves);
			var alreadyappliedMovesCount = restartingPoint.AppliedMoves.Count;

			//skip already applied moves
			for (int i = 0; i < alreadyappliedMovesCount; i++)
			{
				plannedMoves.Dequeue();
			}

			PrioritizeRecalculationStrategy faithful = new PrioritizeRecalculationStrategy(_config);
			var newSolution = faithful.Recalculate(restartingPoint, previousState, plannedMoves, eventType);

			if (newSolution is null)
				throw new InvalidOperationException("No solution found from restart point");

			return newSolution;
		}
	}
}
