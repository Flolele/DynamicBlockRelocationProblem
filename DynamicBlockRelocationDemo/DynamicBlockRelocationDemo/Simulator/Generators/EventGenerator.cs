using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Events;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators
{
    public enum EEventType
    {
		ExpectedExecutionEvent = 0,
		NewBlockEvent,
        MissmoveEvent, 
        BlockTargetUpdateEvent
        //add another event types when adding a new dervied class
    }

    internal class EventGenerator
    {
        private Dictionary<EEventType, double> EventProbabilities;
        private RandomGenerator randomGen { get; init; }
        private BlockConstraintGenerator BlockConstraintGenerator { get; init; }
        private MissmoveGenerator MissmoveGenerator { get; init; }

        public EventGenerator(Dictionary<EEventType, double> probalities)
        {
            randomGen = RandomGenerator.Instance;
            BlockConstraintGenerator = new BlockConstraintGenerator();
            MissmoveGenerator = new MissmoveGenerator();
			EventProbabilities = probalities;
		}

        private EEventType MapProbablityToEventType(double probablity)
        {
            var totalWeight = EventProbabilities.Values.Sum();
            if (probablity < 0 || probablity > totalWeight) throw new InvalidOperationException($"Cannot convert {probablity} to a Event");

            double cumulativeWeight = 0.0;
            foreach (var mapping in EventProbabilities)
            {
                cumulativeWeight += mapping.Value;
                if (probablity <= cumulativeWeight)
                {
                    return mapping.Key;
                }
            }
            throw new InvalidOperationException("Couldn't map probability to an event, check Distrubition");
        }

        public Event GenerateRandomEvent(BlockRelocationSimulator simulationState)
        {
            BlockRelocationProblemState currentState = simulationState.GetCurrentState();
            BlockRelocationProblemState nextState = simulationState.GetPlannedNextState();

            //generate a random number to determine the event type
            double totalWeight = EventProbabilities.Values.Sum(); //calculate total weight should usually be 1
            double randomValue = randomGen.NextDouble() * totalWeight; //get a random value between 0 and totalWeight

            var randomEvent = MapProbablityToEventType(randomValue);

            switch (randomEvent)
            {
                case EEventType.NewBlockEvent:
                    return GenerateNewBlockEvent(currentState);
                case EEventType.MissmoveEvent:
                    return GenerateMissmoveEvent(currentState, nextState);
                case EEventType.ExpectedExecutionEvent:
                    return GenerateExpectedExecutionEvent();
                case EEventType.BlockTargetUpdateEvent:
                    return GenerateTargetUpdateEvent(currentState);
                default:
                    throw new InvalidOperationException("Unexpected event type generated");
            }
        }



        private NewBlockEvent GenerateNewBlockEvent(BlockRelocationProblemState currentState)
        {
            //creating a new block, insert position will later be determined in the execute
            var newBlock = new Block();
            newBlock.Id = ++Block._lastId;
            //newBlock.AddConstraints(BlockConstraintGenerator.GenerateConstraints());
 
            return new NewBlockEvent(newBlock);
        }

        private Event GenerateTargetUpdateEvent(BlockRelocationProblemState currentState)
        {
            //get an existing block and update its target position
            var blockToUpdate = currentState.BlockYardManager.GetRemainingBlocks().Where(b => !b.TryGetTargetPosition(out Position _)); //block without target
            if (!blockToUpdate.Any())
                return new ExpectedExecutionEvent(); //no block to update

            var randomBlock = blockToUpdate.ElementAt(randomGen.Next(0, blockToUpdate.Count()));
            return new BlockTargetUpdateEvent(randomBlock);
        }


        private Event GenerateMissmoveEvent(BlockRelocationProblemState currentState, BlockRelocationProblemState nextState)
        {
			try
			{
				if (nextState.AppliedMoves.Any()) // Only generate missmove if there are moves to deviate from
				{
					Move missmove = MissmoveGenerator.GenerateMissmove(currentState, nextState);
					return new MissmoveEvent(missmove);
				}
			}
			catch
			{
				// If anything goes wrong in missmove generation, fall back to expected execution
			}

			return new ExpectedExecutionEvent(); // Fallback when no missmove can be generated
		}

        private ExpectedExecutionEvent GenerateExpectedExecutionEvent()
        {
            return new ExpectedExecutionEvent();
        }
    }
}
