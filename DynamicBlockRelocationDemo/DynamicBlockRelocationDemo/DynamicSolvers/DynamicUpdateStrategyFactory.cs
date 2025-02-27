using DynamicBlockRelocationDemo.BlockRelocation.DynamicImprovements;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.DynamicSolvers;
using DynamicBlockRelocationDemo.DynamicSolvers.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.DynamicMethods
{
	public class DynamicRecalculationStrategyFactory
	{
		private readonly ICostCalculator _costCalculator;
		private readonly BetterStartingPointFinder _restartPointFinder;
		private readonly SimulationConfig _config;

		public DynamicRecalculationStrategyFactory(ICostCalculator costCalculator, SimulationConfig config)
		{
			_costCalculator = costCalculator;
			_restartPointFinder = new BetterStartingPointFinder(costCalculator);
			_config = config;
		}

		public IDynamicUpdateStrategy CreateStrategy(DynamicVariant variant)
		{
			return variant switch
			{
				DynamicVariant.StandardRecalculation => new StandardRecalculationStrategy(_config),
				DynamicVariant.PriotizeRecalculationOld => new PrioritizeRecalculationStrategy(_config),
				DynamicVariant.RepairHeuristic => new RepairHeuristicStrategy(_costCalculator, _config),
				DynamicVariant.AdaptiveRestartPoint => new AdaptiveRestartPointStrategy(_costCalculator, _config),
				DynamicVariant.AdaptivePrioCombined => new PrioritizeWithAdaptiveRestartPointStrategy(_costCalculator, _config),
				DynamicVariant.BacktrackigPoint => new BacktrackingPointStrategy(_costCalculator, _config),
				DynamicVariant.MovePriotization => new PrioritizeRecalculationStrategy(_config),
				_ => throw new ArgumentException($"Unsupported dynamic variant: {variant}")
			};
		}
	}
}
