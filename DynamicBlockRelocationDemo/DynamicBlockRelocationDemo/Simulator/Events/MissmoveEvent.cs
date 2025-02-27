using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Events
{
    internal class MissmoveEvent : Event
    {
        public Move MissedMove { get; init; }

        public MissmoveEvent(Move missedMove)
        {
            MissedMove = missedMove;
        }

        public override void DoExecute(BlockRelocationSimulator state)
        {
            state.HandleMissmove(MissedMove);
        }
    }
}
