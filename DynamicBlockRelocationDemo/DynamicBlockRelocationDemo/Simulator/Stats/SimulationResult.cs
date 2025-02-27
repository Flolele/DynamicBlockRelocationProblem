using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats
{
    public enum SimulationStatus
    {
        Failed,
        Completed,
        Terminated
    }
	public class SimulationResult
	{
		public SimulationStatus Status { get; }
		public SimulationStats Stats { get; }
		public TimeSpan Runtime { get; }
		public List<Move> ExecutedMoves { get; }
		public float TotalMoveCost { get; }
		public float FinalBound { get; }

		public SimulationResult(
			SimulationStatus status,
			SimulationStats stats,
			TimeSpan runtime,
			List<Move> executedMoves,
			ICostCalculator costCalculator)
		{
			Status = status;
			Stats = stats;
			Runtime = runtime;
			ExecutedMoves = executedMoves;
			TotalMoveCost = costCalculator.GetTotalMoveCost(executedMoves);
			FinalBound = stats.GetSnapshots().Last().CurrentBound;
		}
	}
}
