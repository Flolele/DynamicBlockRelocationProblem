using DynamicBlockRelocationDemo.BlockRelocation.DynamicImprovements;
using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreesearchLib;

namespace DynamicBlockRelocationDemo.DynamicSolvers
{
	public interface IDynamicUpdateStrategy
	{
		BlockRelocationProblemState Recalculate(
			BlockRelocationProblemState currentState,
			BlockRelocationProblemState? previousState,
			Queue<Move> plannedMoves,
			EEventType eventType
		);
	}
}