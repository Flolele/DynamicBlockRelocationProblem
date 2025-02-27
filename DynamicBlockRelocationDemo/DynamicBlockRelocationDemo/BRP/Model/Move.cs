using DynamicBlockRelocationDemo.BlockRelocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    public record Move(int CraneId, int BlockId, Position CraneSourcePosition, Position CraneTargetPosition, Position BlockSourcePosition, Position TargetPosition, int ArrivalStackHeight)
    {
        public Move Reverse() => new Move(
            CraneId,
            BlockId,
            CraneTargetPosition,
            CraneSourcePosition,
            TargetPosition,
            BlockSourcePosition,
            ArrivalStackHeight //doesnt matter since reverse moves are only used for undoing
            );

        public override string ToString()
        {
            return $"{BlockId} from {BlockSourcePosition} to {TargetPosition}";
        }
    }


    public class MoveComparer : IEqualityComparer<Move>
    {
        public bool Equals(Move? x, Move? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.BlockId == y.BlockId &&
                   x.BlockSourcePosition == y.BlockSourcePosition &&
                   x.TargetPosition == y.TargetPosition;
            // Not comparing CraneSourcePosition, CraneTargetPosition, or ArrivalStackHeight
        }

        public int GetHashCode(Move obj)
        {
            return HashCode.Combine(
                obj.BlockId,
                obj.BlockSourcePosition,
                obj.TargetPosition
            );
        }
    }
}
