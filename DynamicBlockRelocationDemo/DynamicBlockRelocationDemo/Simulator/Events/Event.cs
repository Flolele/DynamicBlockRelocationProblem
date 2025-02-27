using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator.Events
{
    public abstract class Event
    {
        public void Execute(BlockRelocationSimulator state)
        {
            DoExecute(state);
            //state.Notify(this);
        }

        public abstract void DoExecute(BlockRelocationSimulator state);

    }
}
