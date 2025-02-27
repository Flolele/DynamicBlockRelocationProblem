using DynamicBlockRelocationDemo;
using DynamicBlockRelocationDemo.BlockRelocation;
using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.Simulator;
using System.Drawing;
using TreesearchLib;

public enum TestType
{
	Standard,
	BeamWidthComparison,
	WarehouseSizeComparison,
	RobustnessTest
}


public class TestConfiguration
{
	public BlockRelocationConfig BlockConfig { get; set; }
	public SimulatorTestConfig SimulatorConfig { get; set; }
	public TestType TestType { get; set; } = TestType.Standard;

	public TestConfiguration()
	{
		BlockConfig = new BlockRelocationConfig();
		SimulatorConfig = new SimulatorTestConfig();
	}
}

public class SimulatorTestConfig
{
	public int TotalEvents { get; set; } = 1000;
	public int NumberOfRuns { get; set; } = 1;
	public string OutputPath { get; set; } = "../../../../results/";
	public Dictionary<EEventType, double> EventProbabilities { get; set; }
	public DynamicVariant[] VariantsToTest { get; set; }


	//----------Optional parameters (TestTypes)-----------
	//BeamWidthComparison
	public int[] BeamWidthsToTest { get; set; } = new[] { 50 };
	//WarehouseSizeComparison
	public (int, int, int)[] WarehouseSizesToTest { get; set; } = new[]
	{
		//(6, 3, 3),
		//(7, 3, 3),
		(7, 4, 3),
		//(7, 5, 4)
	};
	//RobustnessTest
	public int[] SeedsToTest { get; set; } = new[] { 1342 };
	//---------------------------------------------------

	public SimulatorTestConfig()
	{
		EventProbabilities = new Dictionary<EEventType, double>
		{
			{ EEventType.ExpectedExecutionEvent, 0.5 },
			{ EEventType.NewBlockEvent, 0.2 },
			{ EEventType.MissmoveEvent, 0.1 },
			{ EEventType.BlockTargetUpdateEvent, 0.2 }
		};

		VariantsToTest = new[]
		{
			DynamicVariant.StandardRecalculation,
			//DynamicVariant.PriotizeRecalculation,
			DynamicVariant.MovePriotization,
			DynamicVariant.AdaptiveRestartPoint,
			DynamicVariant.AdaptivePrioCombined,
			DynamicVariant.BacktrackigPoint,
			DynamicVariant.RepairHeuristic
		};
	}
}

internal class Program
{
	private static void Main(string[] args)
	{
		var config = CreateDefaultConfiguration();

		switch (config.TestType)
		{
			case TestType.Standard:
				RunStandardTest(config);
				break;
			case TestType.BeamWidthComparison:
				RunBeamWidthComparison(config);
				break;
			case TestType.WarehouseSizeComparison:
				RunWarehouseSizeComparison(config);
				break;
			case TestType.RobustnessTest:
				RunRobustnessTest(config);
				break;
		}
	}

	private static TestConfiguration CreateDefaultConfiguration()
	{
		return new TestConfiguration
		{
			BlockConfig = new BlockRelocationConfig
			{
				Length = 6,
				Width = 3,
				Height = 3,
				MoveCostPerUnit = 1f,
				PickupCost = 5f,
				PlacementCost = 5f,
				CraneStartPosition = new Position(0, 0, 0),
				CraneOperationalAreaEnd = new Position(5, 2, 2), // length-1, height-1, width-1
				CraneOperationalAreaStart = new Position(0, 0, 0),
				RuntimeLimit = TimeSpan.FromSeconds(3000),
				BeamWidth = 20
			}
		};
	}

	private static void RunTests(TestConfiguration config)
	{
		var (brp, costCalculator) = InitializeProblem(config.BlockConfig);

		// Uncomment the test you want to run
		// TestBeamsearch(brp, config.BlockConfig);
		// TestSimulator(brp, config.BlockConfig, costCalculator);
		TestSimulatorDifferences(brp, config, costCalculator);
	}

	private static void RunStandardTest(TestConfiguration config)
	{
		var (brp, costCalculator) = InitializeProblem(config.BlockConfig);
		var runner = new VariantComparisonRunner(brp, costCalculator);

		runner.AddTestConfiguration(
			config.SimulatorConfig.VariantsToTest,
			config.SimulatorConfig.TotalEvents,
			config.SimulatorConfig.EventProbabilities
		);

		runner.RunComparisons(
			config.SimulatorConfig.NumberOfRuns,
			config.SimulatorConfig.OutputPath + "standard_results.json"
		);
	}

	private static void RunBeamWidthComparison(TestConfiguration config)
	{
		var (brp, costCalculator) = InitializeProblem(config.BlockConfig);

		foreach (var beamWidth in config.SimulatorConfig.BeamWidthsToTest)
		{

			config.BlockConfig.BeamWidth = beamWidth;
			var runner = new VariantComparisonRunner(brp, costCalculator);

			runner.AddTestConfiguration(
				config.SimulatorConfig.VariantsToTest,
				config.SimulatorConfig.TotalEvents,
				config.SimulatorConfig.EventProbabilities,
				config.BlockConfig.BeamWidth
			);

			runner.RunComparisons(
				config.SimulatorConfig.NumberOfRuns,
				config.SimulatorConfig.OutputPath + $"beam_width_{beamWidth}_results.json"
			);
		}
	}

	private static void RunWarehouseSizeComparison(TestConfiguration config)
	{
		foreach (var (length, width, height) in config.SimulatorConfig.WarehouseSizesToTest)
		{
			config.BlockConfig.Length = length;
			config.BlockConfig.Width = width;
			config.BlockConfig.Height = height;

			config.BlockConfig.CraneOperationalAreaEnd = new Position(length - 1, height - 1, width - 1);
			config.BlockConfig.CraneOperationalAreaStart = new Position(0, 0, 0);

			config.BlockConfig.RuntimeLimit = TimeSpan.FromSeconds(1000);

			var (brp, costCalculator) = InitializeProblem(config.BlockConfig);
			var runner = new VariantComparisonRunner(brp, costCalculator);

			runner.AddTestConfiguration(
				config.SimulatorConfig.VariantsToTest,
				config.SimulatorConfig.TotalEvents,
				config.SimulatorConfig.EventProbabilities
			);

			runner.RunComparisons(
				config.SimulatorConfig.NumberOfRuns,
				config.SimulatorConfig.OutputPath + $"warehouse_{length}x{width}x{height}.json"
			);
		}
	}

	private static void RunRobustnessTest(TestConfiguration config)
	{
		var (brp, costCalculator) = InitializeProblem(config.BlockConfig);
		var runner = new VariantComparisonRunner(brp, costCalculator);

		runner.AddTestConfiguration(
			config.SimulatorConfig.VariantsToTest,
			config.SimulatorConfig.TotalEvents,
			config.SimulatorConfig.EventProbabilities
		);

		foreach (var seed in config.SimulatorConfig.SeedsToTest)
		{
			runner.RunComparisons(
				1,  // One run per seed
				config.SimulatorConfig.OutputPath + $"seed_{seed}_results.json",
				startingSeed: seed
			);
		}
	}

	private static (BlockRelocationProblemState, ICostCalculator) InitializeProblem(BlockRelocationConfig config)
	{
		var costCalculator = new CraneCostCalculator(config.MoveCostPerUnit, config.PickupCost, config.PlacementCost);
		var containerArea = new BlockArea(config.Length, config.Width, config.Height);

		// Initialize blocks
		//BlockInitializer.InitializeBlocks(containerArea);
		BlockInitializer.LoadBlockLayoutFromFile(containerArea, "block_layout.txt");

		var crane = new YardCrane(1, config.CraneStartPosition);
		var blockYardManager = new BlockYardManager(containerArea, costCalculator);
		blockYardManager.AssignCrane(crane, config.CraneOperationalAreaStart, config.CraneOperationalAreaEnd);

		return (new BlockRelocationProblemState(blockYardManager, costCalculator), costCalculator);
	}

	private static void TestBeamsearch(BlockRelocationProblemState brp, BlockRelocationConfig config)
	{
		var control = Minimize.Start(brp)
			.WithRuntimeLimit(config.RuntimeLimit)
			.BeamSearch(config.BeamWidth,
				state =>
				{
					var boundValue = state.Bound.Value;
					var blockedBlocks = state.AppliedMoves.Any()
						? state.BlockYardManager.GetBlockedBlocks()
						: 0;
					return boundValue + (blockedBlocks * 10);
				}
			);

		ProcessBeamsearchResults(control);
	}

	private static void ProcessBeamsearchResults(SearchControl<BlockRelocationProblemState, Move, Minimize>? control)
	{
		if (control == null)
		{
			Console.WriteLine("Couldn't find a valid solution within the Runtime of the beamsearch");
			return;
		}
		var best = control.BestQualityState;
		if (best == null)
		{
			Console.WriteLine("Couldn't find a valid solution within the Runtime of the beamsearch");
			return;
		}

		Console.WriteLine($"Best solution quality: {best.Quality}");
		Console.WriteLine("Moves to solve the problem:");
		foreach (var move in best.AppliedMoves.Reverse())
		{
			Console.WriteLine($"Block with Id {move.BlockId} from Position {move.BlockSourcePosition} to {move.TargetPosition}");
		}
		Console.WriteLine($"All constraints fulfilled: {best.BlockYardManager.AllFinshingCriteriasFullfilled()}");
	}

	private static void TestSimulator(BlockRelocationProblemState brp, BlockRelocationConfig config, ICostCalculator costCalculator)
	{
		var control = Minimize.Start(brp)
			.WithRuntimeLimit(config.RuntimeLimit)
			.BeamSearch(config.BeamWidth, state => state.Bound.Value);

		var best = control.BestQualityState;
		if (best == null)
		{
			Console.WriteLine("Couldn't find a valid solution within the Runtime of the beamsearch for the simulator");
			return;
		}

		var simulationConfig = new SimulationConfig(costCalculator, DynamicVariant.PriotizeRecalculationOld);
		var simulator = new BlockRelocationSimulator(brp, best, simulationConfig);
		simulator.Run();
	}

	private static void TestSimulatorDifferences(BlockRelocationProblemState brp, TestConfiguration config, ICostCalculator costCalculator)
	{
		var comparisonRunner = new VariantComparisonRunner(brp, costCalculator);

		comparisonRunner.AddTestConfiguration(
			variants: config.SimulatorConfig.VariantsToTest,
			totalEvents: config.SimulatorConfig.TotalEvents,
			eventProbabilities: config.SimulatorConfig.EventProbabilities
		);

		comparisonRunner.RunComparisons(
			numberOfRuns: config.SimulatorConfig.NumberOfRuns,
			baseOutputPath: config.SimulatorConfig.OutputPath
		);

		Console.WriteLine($"\nSimulation results exported to {config.SimulatorConfig.OutputPath}");
		Console.WriteLine("Run the Python visualization script to analyze results!");
	}
}