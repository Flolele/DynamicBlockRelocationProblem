using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators
{
    internal enum ESupportedConstraints
    {
        TargetPositionConstraint
    };

    public class BlockConstraintGenerator
    {
        private RandomGenerator randomGenerator { get; init; }
        public BlockConstraintGenerator()
        {
            randomGenerator = RandomGenerator.Instance;
        }

        private readonly Dictionary<ESupportedConstraints, double> _constraintProbabilities = new Dictionary<ESupportedConstraints, double>
        {
            { ESupportedConstraints.TargetPositionConstraint, 0.5 } //chance to add a target position constraint
        };

        public List<IFinishingCriteria<Block>> GenerateConstraints()
        {
            List<IFinishingCriteria<Block>> constraints = new List<IFinishingCriteria<Block>>();

            foreach (var constraintProbability in _constraintProbabilities)
            {
                if (randomGenerator.NextDouble() <= constraintProbability.Value)
                {
                    constraints.Add(CreateConstraint(constraintProbability.Key));
                }
            }
            return constraints;
        }

        private IFinishingCriteria<Block> CreateConstraint(ESupportedConstraints constraintType)
        {
            switch (constraintType)
            {
                case ESupportedConstraints.TargetPositionConstraint:
                    return CreateBlockTargetPositionConstraint();
                default:
                    throw new InvalidOperationException("Unexpected event type generated");
            }
        }

        private BlockTargetPositionConstraint CreateBlockTargetPositionConstraint()
        {
            //currently target can only be void or no target (no target postion constraint)
            return new BlockTargetPositionConstraint(BlockArea.VOID_POSITION);
        }
    }
}
