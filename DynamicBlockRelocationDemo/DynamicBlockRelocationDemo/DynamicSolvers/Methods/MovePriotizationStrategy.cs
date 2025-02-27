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

	public class PrioritizeRecalculationStrategy : IDynamicUpdateStrategy
	{
		// Constants from equations 3.3 and 3.4
		private const float ALPHA = 20.0f;
		private const float W_BLOCK = 0.5f;
		private const float W_SOURCE = 0.20f;
		private const float W_TARGET = 0.25f;
		private const float W_CRANE = 0.05f;
		private const int WINDOW_SIZE = 5;

		private SimulationConfig _config;

		public PrioritizeRecalculationStrategy(SimulationConfig config)
		{
			_config = config;
		}

		private readonly Dictionary<string, float> _similarityCache = new();

		public BlockRelocationProblemState Recalculate(
			BlockRelocationProblemState currentState,
			BlockRelocationProblemState? previousState,    // Not used in prioritization
			Queue<Move> plannedMoves,                     // These are our Mprevious
			EEventType eventType)                         // Not used in prioritization
		{
			_similarityCache.Clear();
			PrecalculateSimilarities(plannedMoves);


			return currentState.BeamSearch(
				beamWidth: _config.BeamWidth,
				rank: state => CalculatePriority(state, plannedMoves),
				runtime: _config.DynamicUpdateTimeout
			);
		}
		private void PrecalculateSimilarities(IEnumerable<Move> moves)
		{
			foreach (var move in moves)
			{
				var key = GetMoveKey(move);
				_similarityCache[key] = 1;
			}
		}

		private float CalculatePriority(
			BlockRelocationProblemState state,
			IEnumerable<Move> previousMoves)
		{
			var boundValue = state.Bound.Value;
			var blockedBlocks = state.AppliedMoves.Any()
						? state.BlockYardManager.GetBlockedBlocks()
						: 0;

			var newMove = state.AppliedMoves.FirstOrDefault();
			if (newMove == null) return boundValue;

			// Calculate simmax(m,Mprevious) with caching
			int windowStart = state.AppliedMoves.Count;
			float maxSimilarity = GetMaxSimilarity(newMove, previousMoves, windowStart);

			return boundValue + blockedBlocks * 10 - ALPHA * maxSimilarity;
		}

		private float GetMaxSimilarity(Move move, IEnumerable<Move> previousMoves, int windowStart)
		{
			var moveKey = GetMoveKey(move);

			float maxSimilarity = 0;

			//try to get it out of cache first
			if (_similarityCache.TryGetValue(moveKey, out float similarity))
			{
				return Math.Max(maxSimilarity, similarity);
			}

			//introduce sliding window
			foreach (var previousMove in previousMoves.Skip(windowStart).Take(WINDOW_SIZE))
			{

				// Calculate and cache if not found
				similarity = CalculateMoveSimilarity(move, previousMove);
				_similarityCache[moveKey] = similarity;

				maxSimilarity = Math.Max(maxSimilarity, similarity);
			}

			return maxSimilarity;
		}

		private float CalculateMoveSimilarity(Move m1, Move m2)
		{
			float similarity = 0;

			if (m1.BlockId == m2.BlockId)          // Iblock
				similarity += W_BLOCK;
			if (m1.BlockSourcePosition == m2.BlockSourcePosition)  // Isource
				similarity += W_SOURCE;
			if (m1.TargetPosition == m2.TargetPosition)  // Itarget
				similarity += W_TARGET;
			if (m1.CraneSourcePosition == m2.CraneSourcePosition)    // Icrane
				similarity += W_CRANE;

			return similarity;
		}

		private static string GetMoveKey(Move move) => $"{move.BlockId}:{move.BlockSourcePosition}:{move.TargetPosition}";

	}
}
