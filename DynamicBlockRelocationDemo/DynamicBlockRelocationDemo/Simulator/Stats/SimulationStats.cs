using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Events;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TreesearchLib;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats
{
	public class SimulationStats
	{
		private readonly List<StateSnapshot> _snapshots = new();
		private readonly int _totalEvents;
		private int? _previousBound;

		public SimulationStats(int totalEvents)
		{
			_totalEvents = totalEvents;
		}

		public void RecordState(
			BlockRelocationSimulator simulator,
			BlockRelocationProblemState state,
			EEventType eventType,
			Move? executedMove = null,
			bool? moveSuccess = null,
			TimeSpan? recalculationTime = null,
			int? newSolutionMoves = null)
		{
			var currentBound = state.Bound.Value;
			int boundChange = _previousBound.HasValue ? (currentBound - _previousBound.Value) : 0;
			_previousBound = currentBound;

			var snapshot = new StateSnapshot
			{
				EventNumber = _snapshots.Count + 1,
				Timestamp = DateTime.Now,
				EventType = eventType,

				// Warehouse metrics
				WarehouseUtilization = state.BlockYardManager.GetWarehouseUtilization(),
				ArrivalStackHeight = state.BlockYardManager.GetArrivalStackCount(),
				ArrivalStackIdealHeight = simulator._config.CostCalculator.IdealArrivalQueueSize,
				BlockedBlocks = state.BlockYardManager.GetBlockedBlocks(),
				EmptyStacks = state.BlockYardManager.GetEmptyStacks(),
				StackHeightDistribution = state.BlockYardManager.GetStackHeightDistribution(),

				// Solution metrics
				CurrentBound = currentBound,
				BoundChange = boundChange,
				PlannedMovesRemaining = simulator.PlannedMoves.Count,

				// Move information
				ExecutedMove = executedMove,
				MoveSuccessful = moveSuccess,
				MoveCost = executedMove != null ? simulator._config.CostCalculator.CalculateMovementCost(executedMove) : null,
				IsExpectedMove = eventType == EEventType.ExpectedExecutionEvent,
				TotalCostSoFar = simulator.ExecutedMoves.Any() ?
					simulator._config.CostCalculator.GetTotalMoveCost(simulator.ExecutedMoves) : 0,
				TotalMovesExecuted = simulator.ExecutedMoves.Count,

				// Recalculation metrics
				RecalculationTimeMs = recalculationTime?.TotalMilliseconds,
				NewSolutionMoveCount = newSolutionMoves
			};

			_snapshots.Add(snapshot);
		}

		public string ExportToJson()
		{
			var data = new
			{
				totalEvents = _totalEvents,

				snapshots = _snapshots.Select(s => new
				{
					eventNumber = s.EventNumber,
					timestamp = s.Timestamp,
					eventType = s.EventType,

					warehouseMetrics = new
					{
						utilization = s.WarehouseUtilization,
						arrivalStackHeight = s.ArrivalStackHeight,
						arrivalStackIdealHeight = s.ArrivalStackIdealHeight,
						blockedBlocks = s.BlockedBlocks,
						emptyStacks = s.EmptyStacks,
						stackHeightDistribution = s.StackHeightDistribution
					},

					solutionMetrics = new
					{
						currentBound = s.CurrentBound,
						boundChange = s.BoundChange,
						plannedMovesRemaining = s.PlannedMovesRemaining
					},

					moveInfo = s.ExecutedMove != null ? new
					{
						blockId = s.ExecutedMove.BlockId,
						source = s.ExecutedMove.BlockSourcePosition,
						target = s.ExecutedMove.TargetPosition,
						success = s.MoveSuccessful,
						cost = s.MoveCost,
						isExpected = s.IsExpectedMove,
						totalCostSoFar = s.TotalCostSoFar,
						totalMoves = s.TotalMovesExecuted
					} : null,

					recalculationInfo = s.RecalculationTimeMs.HasValue ? new
					{
						timeMs = s.RecalculationTimeMs.Value,
						newMoveCount = s.NewSolutionMoveCount
					} : null
				}).ToList()
			};

			return JsonSerializer.Serialize(
				data,
				new JsonSerializerOptions { WriteIndented = true }
			);
		}

		public List<StateSnapshot> GetSnapshots() => _snapshots;
	}
}
