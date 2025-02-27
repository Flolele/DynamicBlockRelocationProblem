using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Events
{
    public class BlockTargetUpdateEvent : Event
    {
        Block BlockToUpdate { get; init; }
        public BlockTargetUpdateEvent(Block block)
        {
            BlockToUpdate = block;
        }

        public override void DoExecute(BlockRelocationSimulator state)
        {
            state.UpdateBlockTarget(BlockToUpdate);
        }
    }
}
