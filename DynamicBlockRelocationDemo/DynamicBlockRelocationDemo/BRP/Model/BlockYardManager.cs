using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    public class BlockYardManager
    {
        private IBlockArea _blockArea;
        private List<ICrane> _cranes;
        private ICostCalculator _costCalculator;
        public Dictionary<int, Position> CraneStartingPositions { get; private set; }

        public BlockYardManager(IBlockArea blockArea, ICostCalculator costCalculator)
        {
            _blockArea = blockArea;
            _costCalculator = costCalculator;
            _cranes = new List<ICrane>();
            CraneStartingPositions = new Dictionary<int, Position>();
        }
        public IEnumerable<Move> GetAllPossibleMoves() => _cranes.SelectMany(crane => (GetAllPossibleMovesByCrane(crane)));
        public IEnumerable<Block> GetRemainingBlocks() => _blockArea.GetRemainingBlocks();
        public IEnumerable<Position> GetFreePositions() => _blockArea.GetFreePositions();

        public Position GetPosition(Block block) => _blockArea.GetPosition(block);
        public Position GetStartPositionOfCranes(int CraneId) => CraneStartingPositions[CraneId];

        public bool TryPlaceBlock(Position insertPosition, Block block) => _blockArea.TryPlaceBlockToPosition(insertPosition, block);
        public bool AllFinshingCriteriasFullfilled() => _blockArea.AllFinshingCriteriasFullfilled();
        public bool IsBlockAccessible(Block block) => _blockArea.IsBlockAccessible(block);
        public IEnumerable<Block> GetBlocksAbove(Block block) => _blockArea.GetBlocksAbove(block);
        public Block? PeekArrivalStack() => _blockArea.PeekArrivalStack();
        public int GetArrivalStackCount() => _blockArea.GetArrivalStackCount();

        public string GetDimensions() => _blockArea.GetDimensions();



		public IEnumerable<Move> GetAllPossibleMovesByCrane(ICrane crane)
        {
            //get all Blocks that are within reach of the Crane
            var blocksInReach = FilterBlocksInRange(crane, _blockArea.GetTopBlocks());

            //list of move and distance to the VOID_POSITION to priotize accordingly
            var movesWithCost = new List<(Move move, float priority)>();

            foreach (var block in blocksInReach)
            {
                foreach (var targetPosition in FilterPositionsInRange(crane, _blockArea.GetFreePositions().Where(pos => !ArePositionsOnSameStack(_blockArea.GetPosition(block), pos))))
                {
                    if(!block.TryGetTargetPosition(out Position _) && targetPosition == BlockArea.VOID_POSITION)
                    {
                        continue; //not allowed to move to void if target position is set
                    }

                    if(GetPosition(block) == BlockArea.ARRIVAL_QUEUE_POSITION && targetPosition == BlockArea.VOID_POSITION)
                    {
                        continue; //not allowed to move blocks from arrival stack immediately to void
                    }

                    Position sourcePos = _blockArea.GetPosition(block);
                    var currentMove = new Move(crane.Id, block.Id, crane.CurrentPosition, targetPosition, sourcePos, targetPosition, GetArrivalStackCount());
                    Position? goalDestinationPosition = null;
                    if (block.TryGetTargetPosition(out Position endTarget)) 
                    {
                        goalDestinationPosition = endTarget;
                    }
                    float priority = _costCalculator.CalculateMovePriority(currentMove, goalDestinationPosition, _blockArea);         
                    movesWithCost.Add((currentMove, priority));
                }
            }
            return movesWithCost.OrderBy(x => x.priority).Select(x => x.move);
        }
        public void ApplyMove(Move move)
        {
            ICrane executerCrane = _cranes.First(crane => crane.Id == move.CraneId);
            if (executerCrane.IsReachableByCrane(move.BlockSourcePosition) && executerCrane.IsReachableByCrane(move.TargetPosition))
            {
                executerCrane.TryMoveTo(move.BlockSourcePosition); //cranes moves to block
                if (_blockArea.TryApplyMove(move)) //block gets moved
                {
                    executerCrane.TryMoveTo(move.CraneTargetPosition); //crane moves to target destination
                }
                else
                {
                    executerCrane.TryMoveTo(move.CraneSourcePosition); //move back to original position
                    throw new InvalidOperationException("Couldn't apply move");
                }
            }
            else
            {
                throw new InvalidOperationException("Crane couldn't reach block or the  target position");
            }

        }
        public bool TryMoveBlock(ICrane crane, Position sourcePosition, Position targetPosition)
        {
            if (!(crane.IsReachableByCrane(sourcePosition) && crane.IsReachableByCrane(targetPosition)))
                return false;

            if (_blockArea.TryPickupBlockFromPosition(sourcePosition, out Block block))
            {
                if (_blockArea.TryPlaceBlockToPosition(targetPosition, block))
                    return true;

                //if placement fails, put the block back
                _blockArea.TryPlaceBlockToPosition(sourcePosition, block);
            }
            return false;
        }
        public void AssignCrane(ICrane crane, Position point1, Position point2)
        {
            if (!_blockArea.IsAreaWithinBounds(point1, point2))
                throw new InvalidOperationException($"Area of {point1}, {point2} is not within the bounds of the area");

            _cranes.Add(crane);
            crane.SetOperationalArea(point1, point2);
            CraneStartingPositions.Add(crane.Id, crane.CurrentPosition);
        }

        public void AddToArrivalStack(Block block)
        {
            _blockArea.AddToArrivalStack(block);
        }

        internal Position GetPositionOfCrane(int closestCraneId)
        {
            return _cranes.First(crane => crane.Id == closestCraneId).CurrentPosition;
        }

        public string GetStateHash()
        {
            return _blockArea.GetStateHash();
        }

        public int GetBlockedBlocks()
        {
            return _blockArea.GetBlockedBlocksAmount();
        }

        public BlockYardManager Clone()
        {
            BlockYardManager clone = new BlockYardManager(
                _blockArea.Clone(),
                _costCalculator     //reusing the same cost calculator, shared reference!
            );

            //clone the cranes
            clone._cranes = new List<ICrane>(_cranes.Select(crane => crane.Clone()));
            clone.CraneStartingPositions = new Dictionary<int, Position>(CraneStartingPositions);
            return clone;
        }
        #region Helper Methods
        private IEnumerable<Block> FilterBlocksInRange(ICrane crane, IEnumerable<Block> blocks)
        {
            return blocks.Where(block => crane.IsReachableByCrane(_blockArea.GetPosition(block)));
        }
        private IEnumerable<Position> FilterPositionsInRange(ICrane crane, IEnumerable<Position> positions)
        {
            return positions.Where(pos => crane.IsReachableByCrane(pos));
        }
        private bool ArePositionsOnSameStack(Position pos1, Position pos2)
        {
            return pos1.X == pos2.X && pos1.Z == pos2.Z;
        }
        #endregion

        #region Simulation Helper Methods
        public Position? GenerateRandomInsertPosition()
        {
            return _blockArea.GenerateRandomInsertPosition();
        }

        public Block GetBlock(int blockid)
        {
            return _blockArea.GetBlock(blockid);

        }

        public int GetTotalPositions()
        {
            return _blockArea.GetTotalPositions();
        }

        public int GetOccupiedPositions()
        {
            return _blockArea.GetRemainingBlocks().Count();
        }

        public float GetWarehouseUtilization()
        {
            var totalSpace = GetTotalPositions() + _costCalculator.IdealArrivalQueueSize;
            var occupiedSpace = GetOccupiedPositions();
            return (float)occupiedSpace / totalSpace;
        }

        public float GetAverageStackHeight()
        {
            var stacks = _blockArea.GetStackHeights();
            return (float)( stacks.Any() ? stacks.Average() : 0);
        }

        public int GetEmptyStacks()
        {
            return _blockArea.GetStackHeights().Count(h => h == 0);
        }

        public Dictionary<int, int> GetStackHeightDistribution()
        {
            var heights = _blockArea.GetStackHeights();
            return heights.GroupBy(h => h)
                         .ToDictionary(g => g.Key, g => g.Count());
        }

        #endregion
    }
}
