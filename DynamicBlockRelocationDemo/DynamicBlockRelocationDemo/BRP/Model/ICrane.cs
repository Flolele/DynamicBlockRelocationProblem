using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    public interface ICrane
    {
        int Id { get; }
        Position CurrentPosition { get; }
        bool IsReachableByCrane(Position position);
        bool TryMoveTo(Position position);
        void SetOperationalArea(Position point1, Position point2);
        ICrane Clone();
    }
    //TODO support Quay Cranes (outside, adding/removing blocks) aswell as Yard Cranes (inside the dockyard)
}
