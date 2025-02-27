using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    public class BlockTargetPositionConstraint : IFinishingCriteria<Block>
    {
        public Position TargetPosition { get; set; }

        public BlockTargetPositionConstraint(Position targetPosition) { TargetPosition = targetPosition; }

        public bool IsSatisfied(Block block, BlockArea containerArea)
        {
            var blocksCurrentPosition = containerArea.GetPosition(block);
            return blocksCurrentPosition == TargetPosition;
        }
    }
}
