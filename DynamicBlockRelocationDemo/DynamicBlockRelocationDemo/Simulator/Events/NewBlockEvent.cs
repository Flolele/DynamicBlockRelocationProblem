using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Events
{
    internal class NewBlockEvent : Event
    {
        public Block NewBlock { get; init; }

        public NewBlockEvent(Block newBlock)
        {
            NewBlock = newBlock;
        }

        public override void DoExecute(BlockRelocationSimulator state)
        {
            state.PlaceNewBlock(NewBlock);

        }

    }
}
