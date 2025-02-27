using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Events
{
    internal class ExpectedExecutionEvent : Event
    {
        public ExpectedExecutionEvent()
        {
        }

        public override void DoExecute(BlockRelocationSimulator state)
        {
            state.ApplyNextExpectedMove();
        }
    }
}
