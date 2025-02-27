using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    public interface IFinishingCriteria<TContext>
    {
        bool IsSatisfied(TContext context, BlockArea containerArea);
    }
}
