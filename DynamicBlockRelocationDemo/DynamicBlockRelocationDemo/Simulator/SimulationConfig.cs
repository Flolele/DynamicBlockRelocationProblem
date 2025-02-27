using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator
{
    public class SimulationConfig
    {
        // Core simulation parameters
        public int TotalEvents { get; init; } = 1000;  // Fixed number of events to simulate
        public ICostCalculator CostCalculator { get; init; }
        public DynamicVariant DynamicVariant { get; init; }

        // Event generation probabilities
        public Dictionary<EEventType, double> EventProbabilities { get; init; } = new()
        {
            { EEventType.ExpectedExecutionEvent, 0.4 },
            { EEventType.NewBlockEvent, 0.2 },
            { EEventType.MissmoveEvent, 0.2 },
            { EEventType.BlockTargetUpdateEvent, 0.2 }
        };



        // Algorithm parameters
        public int BeamWidth { get; init; } = 2;
        public TimeSpan InitialSolutionTimeout { get; init; } = TimeSpan.FromSeconds(1000);
        public TimeSpan DynamicUpdateTimeout { get; init; } = TimeSpan.FromSeconds(100);


        // Random seed for reproducibility
        public int RandomSeed { get; init; } = 1337;

        public SimulationConfig(ICostCalculator costCalculator, DynamicVariant dynamicVariant)
        {
            CostCalculator = costCalculator;
            DynamicVariant = dynamicVariant;
        }
    }

}
