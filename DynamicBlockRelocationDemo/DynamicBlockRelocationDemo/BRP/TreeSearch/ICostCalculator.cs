using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.TreeSearch
{
    public interface ICostCalculator
    {
        float CalculateMovePriority(Move move, Position? goalTargetPosition, IBlockArea blockArea);
        float CalculateMovementCost(Move move);
        public float MinimumPickupCost { get; }
        public float MinmumPlacementCost { get; }
        public float MoveCostPerUnit { get; }
        public float ArrivalStackExcessPenalty { get; }
        public int IdealArrivalQueueSize { get; }

        public float GetTotalMoveCost(IEnumerable<Move> moves)
        {
            float totalCost = 0;
            foreach (var move in moves)
            {
                totalCost += CalculateMovementCost(move);
            }
            return totalCost;
        }
    }

    public class CraneCostCalculator : ICostCalculator
    {
        private readonly float _moveCostPerUnit;
        private readonly float _pickupCost;
        private readonly float _placementCost;
        private readonly float _costIfItsNotToTarget;
        private readonly int _idealArrivalStackHeight = 3;
        private readonly float _arrivalStackPenaltyPerBlock = 30;  // Penalty per block over ideal height

        public float MinimumPickupCost { get; }
        public float MinmumPlacementCost { get; }
        public float MoveCostPerUnit { get; }

        public float ArrivalStackExcessPenalty { get; }

        public int IdealArrivalQueueSize { get; }

        public CraneCostCalculator(float moveCostPerUnit = 1f, float pickupCost = 10f, float placementCost = 10f, float costIfItsNotToTarget = 20f)
        {
            _moveCostPerUnit = moveCostPerUnit;
            _pickupCost = pickupCost;
            _placementCost = placementCost;
            _costIfItsNotToTarget = costIfItsNotToTarget;
            ArrivalStackExcessPenalty = _arrivalStackPenaltyPerBlock;
            IdealArrivalQueueSize = _idealArrivalStackHeight;

            //set minimum costs (these could be different if there's variation in pickup/placement costs)
            MinimumPickupCost = pickupCost;
            MinmumPlacementCost = placementCost;
            MoveCostPerUnit = moveCostPerUnit;
        }


        public float CalculateMovePriority(Move move, Position? targetPosition, IBlockArea blockArea)
        {
            float priority = 0;

            // First priority: Direct void moves for blocks that need to go there
            if (targetPosition == BlockArea.VOID_POSITION)
            {
                if (move.TargetPosition == BlockArea.VOID_POSITION)
                {
                    priority -= 1000;  // Highest priority
                }

            }

            // Second priority: Moving blocks that are blocking blocks that need to go to void
            var blockPosition = move.BlockSourcePosition;
            if (blockPosition != BlockArea.VOID_POSITION && blockPosition != BlockArea.ARRIVAL_QUEUE_POSITION)
            {
                // Check if this block is above any block that needs to go to void
                var blocksBelow = blockArea.GetBlocksBelow(move.BlockId);
                foreach (var blockBelow in blocksBelow)
                {
                    if (blockBelow.TryGetTargetPosition(out Position belowTargetPos)
                        && belowTargetPos == BlockArea.VOID_POSITION)
                    {
                        priority -= 500;  // High priority, but not as high as direct void moves
                        break;
                    }
                }
            }

            // Second priority: Handle very full arrival stack
            if (move.BlockSourcePosition == BlockArea.ARRIVAL_QUEUE_POSITION)
            {
                // Priority increases with stack height but stays less than void moves
                float stackOverflow = move.ArrivalStackHeight;
                priority -= 30 * stackOverflow;  // Progressive priority based on how full it is
            }



            // Small baseline cost for movement to prevent unnecessary moves
            priority += CalculateMovementCost(move);

            return priority;
        }
        public float CalculateMovementCost(Move move)
        {
            float totalCost = 0f;
            //cost to move from the crane's current position to the pick-up position
            totalCost += CalculateManhattanDistance(move.CraneSourcePosition, move.BlockSourcePosition);
            totalCost += _pickupCost;
            //cost to move from the pick-up position to the destination position
            totalCost += CalculateManhattanDistance(move.BlockSourcePosition, move.TargetPosition);
            totalCost += _placementCost;

            //add context penalties
            totalCost += CalculateContextPenalty(move);

            return totalCost;
        }

        private float CalculateContextPenalty(Move move)
        {
            int currentArrivalStackHeight = move.ArrivalStackHeight;
            float penalty = 0f;
			//if(move.BlockSourcePosition == BlockArea.ARRIVAL_QUEUE_POSITION)
			//{
			//    return 0f; //no penalty for moving from arrival stack
			//}
			if (currentArrivalStackHeight > _idealArrivalStackHeight)
            {
                int excess = currentArrivalStackHeight - _idealArrivalStackHeight;
                penalty += _arrivalStackPenaltyPerBlock * excess;  
            }
            return penalty;

        }
        

        //used for herusitc (to ensure underestimate)
        private float CalculateManhattanDistance(Position from, Position to)
        {
            return (Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y) + Math.Abs(to.Z - from.Z)) * MoveCostPerUnit;
        }

        private float CalculateEuclideanDistance(Position from, Position to)
        {
            return (float)Math.Sqrt(
                Math.Pow(to.X - from.X, 2) +
                Math.Pow(to.Y - from.Y, 2) +
                Math.Pow(to.Z - from.Z, 2)
            );
        }
    }
}


