using DynamicBlockRelocationDemo.BlockRelocation.Simulator;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TreesearchLib;

namespace DynamicBlockRelocationDemo.Simulator
{
	public class VariantComparisonRunner
	{
		private readonly BlockRelocationProblemState _initialState;
		private readonly ICostCalculator _costCalculator;
		private readonly List<SimulationConfig> _configsToTest = new();

		public VariantComparisonRunner(BlockRelocationProblemState initialState, ICostCalculator costCalculator)
		{
			_initialState = initialState;
			_costCalculator = costCalculator;
		}

		public void AddTestConfiguration(
			IEnumerable<DynamicVariant> variants,
			int totalEvents = 1000,
			Dictionary<EEventType, double>? eventProbabilities = null,
			int beamWidth = 2)
		{
			foreach (var variant in variants)
			{
				var config = new SimulationConfig(_costCalculator, variant)
				{
					TotalEvents = totalEvents,
					EventProbabilities = eventProbabilities ?? new Dictionary<EEventType, double>
					{
						{ EEventType.ExpectedExecutionEvent, 0.4 },
						{ EEventType.NewBlockEvent, 0.2 },
						{ EEventType.MissmoveEvent, 0.2 },
						{ EEventType.BlockTargetUpdateEvent, 0.2 }
					},
					BeamWidth = beamWidth
				};
				_configsToTest.Add(config);
			}
		}

		public void RunComparisons(int numberOfRuns, string baseOutputPath, bool useDifferentSeeds = false, int startingSeed = 1337)
		{
			var allResults = new List<ComparisonRun>();
			//calculate initial solution
			var solution = _initialState.BeamSearch(10, state => state.Bound.Value, runtime: TimeSpan.FromSeconds(1000));
			if (solution == null)
				throw new InvalidOperationException("Could not find initial solution");

			for (int run = 0; run < numberOfRuns; run++)
			{
				Console.WriteLine($"\nStarting run {run + 1}/{numberOfRuns}");
				var currentSeed = useDifferentSeeds ? startingSeed + run : startingSeed;
				var runResults = new Dictionary<string, SimulationResult>();

				foreach (var config in _configsToTest)
				{
					Console.WriteLine($"Testing {config.DynamicVariant}...");

					// Reset RNG for consistent event generation
					RandomGenerator.Instance.SetSeed(currentSeed);

					var simulator = new BlockRelocationSimulator(
						(BlockRelocationProblemState)_initialState.Clone(),
						solution,
						config);

					var result = simulator.Run();
					var configId = GenerateConfigId(config);
					runResults[configId] = result;
				}

				allResults.Add(new ComparisonRun(currentSeed, runResults));
			}

			var dimensions = _initialState.BlockYardManager.GetDimensions();
			var beamWidth = _configsToTest.First().BeamWidth;

			string outputPath = baseOutputPath;
			if (outputPath.Contains("{beam_width}"))
				outputPath = outputPath.Replace("{beam_width}", beamWidth.ToString());
			if (outputPath.Contains("{dimensions}"))
				outputPath = outputPath.Replace("{dimensions}", dimensions);
			if (outputPath.Contains("{seed}"))
				outputPath = outputPath.Replace("{seed}", startingSeed.ToString());

			// Export results
			ExportResults(allResults, outputPath);
		}

		private string GenerateConfigId(SimulationConfig config)
		{
			var probString = string.Join("_", config.EventProbabilities.Select(kvp => $"{kvp.Key}-{kvp.Value:F1}"));
			return $"{config.DynamicVariant}_{config.TotalEvents}_{probString}";
		}

		private void ExportResults(List<ComparisonRun> results, string outputPath)
		{
			var data = new
			{
				warehouse = new
				{
					dimensions = _initialState.BlockYardManager.GetDimensions(),
					totalPositions = _initialState.BlockYardManager.GetTotalPositions(),
					width = _initialState.BlockYardManager.GetDimensions().Split('x')[0],
					height = _initialState.BlockYardManager.GetDimensions().Split('x')[1],
					maxStackHeight = _initialState.BlockYardManager.GetDimensions().Split('x')[2]
				},
				configs = _configsToTest.Select(c => new
				{
					id = GenerateConfigId(c),
					variant = c.DynamicVariant.ToString(),
					totalEvents = c.TotalEvents,
					eventProbabilities = c.EventProbabilities,
					beamWidth = c.BeamWidth
				}),
				runs = results.Select(run => new
				{
					seed = run.Seed,
					results = run.Results.ToDictionary(
						kvp => kvp.Key,
						kvp => new
						{
							// Basic stats
							status = kvp.Value.Status.ToString(),
							runtime = kvp.Value.Runtime.TotalSeconds,
							endQuality = kvp.Value.FinalBound,
							totalMoveCost = kvp.Value.TotalMoveCost,

							// Move statistics
							totalMovesExecuted = kvp.Value.ExecutedMoves.Count,
							totalPlannedMoves = kvp.Value.Stats.GetSnapshots().First().PlannedMovesRemaining,
							remainingPlannedMoves = kvp.Value.Stats.GetSnapshots().Last().PlannedMovesRemaining,

							// Event counts
							eventCounts = kvp.Value.Stats.GetSnapshots()
								.GroupBy(s => s.EventType)
								.ToDictionary(
									g => g.Key.ToString(),
									g => g.Count()
								),

							// Recalculation statistics
							// Recalculation statistics
							recalculationCount = kvp.Value.Stats.GetSnapshots()
								.Count(s => s.RecalculationTimeMs.HasValue),
							averageRecalculationTime = kvp.Value.Stats.GetSnapshots()
								.Where(s => s.RecalculationTimeMs.HasValue)
								.Select(s => s.RecalculationTimeMs.Value)
								.DefaultIfEmpty(0)
								.Average(),
							totalRecalculationTime = kvp.Value.Stats.GetSnapshots()
								.Where(s => s.RecalculationTimeMs.HasValue)
								.Sum(s => s.RecalculationTimeMs.Value),

							// Solution quality stats
							initialBound = kvp.Value.Stats.GetSnapshots().First().CurrentBound,
							finalBound = kvp.Value.Stats.GetSnapshots().Last().CurrentBound,
							boundChanges = kvp.Value.Stats.GetSnapshots()
								.Where(s => s.BoundChange.HasValue)
								.Select(s => s.BoundChange.Value)
								.ToList(),

							// Detailed snapshots
							snapshots = kvp.Value.Stats.GetSnapshots().Select(s => new
							{
								s.EventNumber,
								s.Timestamp,
								s.EventType,

								// Warehouse metrics
								s.WarehouseUtilization,
								s.ArrivalStackHeight,
								s.ArrivalStackIdealHeight,
								s.BlockedBlocks,
								s.EmptyStacks,
								s.StackHeightDistribution,

								// Solution metrics
								s.CurrentBound,
								s.BoundChange,
								s.PlannedMovesRemaining,

								// Cost metrics
								s.MoveCost,
								s.TotalCostSoFar,
								s.TotalMovesExecuted,

								// Move info
								ExecutedMove = s.ExecutedMove != null ? new
								{
									s.ExecutedMove.BlockId,
									Source = s.ExecutedMove.BlockSourcePosition,
									Target = s.ExecutedMove.TargetPosition,
									Success = s.MoveSuccessful
								} : null,

								// Recalculation info
								s.RecalculationTimeMs,
								s.NewSolutionMoveCount
							}).ToList()
						})
				})
			};

			File.WriteAllText(
				outputPath,
				JsonSerializer.Serialize(data, new JsonSerializerOptions
				{
					WriteIndented = true,
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				})
			);
		}

		private record ComparisonRun(int Seed, Dictionary<string, SimulationResult> Results);
	}
}
