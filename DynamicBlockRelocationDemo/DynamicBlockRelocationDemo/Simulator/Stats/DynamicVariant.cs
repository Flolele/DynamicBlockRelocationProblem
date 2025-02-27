using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats
{
    public enum DynamicVariant
    {
        StandardRecalculation,
        PriotizeRecalculationOld,
        RepairHeuristic,
        AdaptiveRestartPoint,
		BacktrackigPoint,
		AdaptivePrioCombined,
		PriotizeRecalculation,
		PriotizeRecalculationWithoutRemoving2,
		MovePriotization
	}
}
