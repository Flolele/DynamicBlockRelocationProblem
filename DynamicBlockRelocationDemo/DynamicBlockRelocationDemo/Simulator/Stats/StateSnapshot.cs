using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats
{
	public class StateSnapshot
	{
		// Event information
		public int EventNumber { get; set; }
		public DateTime Timestamp { get; set; }
		public EEventType EventType { get; set; }

		// Warehouse metrics
		public float WarehouseUtilization { get; set; }
		public int ArrivalStackHeight { get; set; }
		public int ArrivalStackIdealHeight { get; set; }
		public int BlockedBlocks { get; set; }
		public int EmptyStacks { get; set; }
		public Dictionary<int, int> StackHeightDistribution { get; set; }

		// Solution metrics
		public int CurrentBound { get; set; }
		public int PlannedMovesRemaining { get; set; }
		public int? BoundChange { get; set; }

		// Move information
		public Move? ExecutedMove { get; set; }
		public bool? MoveSuccessful { get; set; }
		public float? MoveCost { get; set; }
		public bool IsExpectedMove { get; set; }
		public float? TotalCostSoFar { get; set; }
		public int TotalMovesExecuted { get; set; }

		// Recalculation metrics
		public double? RecalculationTimeMs { get; set; }
		public int? NewSolutionMoveCount { get; set; }
	}

}
