using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators
{
    internal enum ESupportedMissmoves
    {
        BlockMisplacement,
        WrongBlockPickedUp //but then moved to the location that was meant for the current container
    };
    public class MissmoveGenerator
    {
        private RandomGenerator randomGenerator { get; init; }
        public MissmoveGenerator()
        {
            randomGenerator = RandomGenerator.Instance;
        }

        private readonly Dictionary<ESupportedMissmoves, double> _missmoveProbabilities = new Dictionary<ESupportedMissmoves, double>
        {
            { ESupportedMissmoves.BlockMisplacement, 1 },
            { ESupportedMissmoves.WrongBlockPickedUp, 0 } //TODO implement wrongblock pick up 
        };


        //generates a missmove based 
        public Move GenerateMissmove(BlockRelocationProblemState currentState, BlockRelocationProblemState nextState)
        {
			if (currentState == null || nextState == null || !nextState.AppliedMoves.Any()) //Added check for empty moves
				throw new InvalidOperationException("Cannot generate Missmove, no moves available or problem already solved");

			Move plannedMove = nextState.AppliedMoves.Peek(); //cannot be empty, if parameters where given correctly

            double randomValue = randomGenerator.NextDouble();

            double cumulativeProbability = 0.0;

            foreach (var missmove in _missmoveProbabilities)
            {
                cumulativeProbability += missmove.Value;
                if (randomValue <= cumulativeProbability)
                {
                    return CreateMissmove(missmove.Key, plannedMove, currentState);
                }
            }
            throw new InvalidOperationException("Missmove generation failed due to probabiliy distrubition fault");
        }

        private Move CreateMissmove(ESupportedMissmoves missmoveType, Move plannedMove, BlockRelocationProblemState currentState)
        {
            switch (missmoveType)
            {
                case ESupportedMissmoves.BlockMisplacement:
                    return CreateBlockMisplacementMove(plannedMove, currentState);
                case ESupportedMissmoves.WrongBlockPickedUp:
                    return CreateWrongBlockPickedUpMove();
                default:
                    throw new InvalidOperationException("Unexpected event type generated");
            }
        }


        public Move CreateBlockMisplacementMove(Move intendedMove, BlockRelocationProblemState state)
        {
            IEnumerable<Position> freePositions = state.BlockYardManager
                    .GetFreePositions()
                    .Except(new List<Position> { intendedMove.TargetPosition, intendedMove.BlockSourcePosition, BlockArea.VOID_POSITION, BlockArea.ARRIVAL_QUEUE_POSITION }) //remove current target and source positions
                    .Where(pos => !(pos.X == intendedMove.BlockSourcePosition.X && pos.Z == intendedMove.BlockSourcePosition.Z)); //remove positions in the same stack
            if (!freePositions.Any())
            {
                //throw new InvalidOperationException("Couldn't generate missplacement, no other free position are available");
                //use inital move then, since if its the only free space it will be the same as the intended move 
                return intendedMove; 
            }


            int randomIndex = randomGenerator.Next(0, freePositions.Count());
            Position newPosition = freePositions.ElementAt(randomIndex);
            
            Move newMove = new Move(intendedMove.CraneId, intendedMove.BlockId, intendedMove.CraneSourcePosition, newPosition, intendedMove.BlockSourcePosition, newPosition, intendedMove.ArrivalStackHeight);
            Console.WriteLine($"Generated Missmove: {newMove}");
            return newMove;
        }

        public Move CreateWrongBlockPickedUpMove()
        {
            //TODO
            throw new NotImplementedException("GenerateWrongBlockPickedUpMove not implemented");
        }

    }
}
