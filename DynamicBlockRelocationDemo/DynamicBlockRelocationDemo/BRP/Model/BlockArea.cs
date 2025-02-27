using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    public class BlockArea : IBlockArea
    {
        public static Position VOID_POSITION = new Position(-1, 0, -1);
        public static Position ARRIVAL_QUEUE_POSITION = new Position(int.MaxValue, 0, int.MaxValue); //this will be adjusted in constructor


        private Queue<Block> _arrivalQueue;
        private Stack<Block>[,] _bufferStacks;
        private Dictionary<int, Position> _currentPositionOfBlock; //map from Block Id to it's position
        private List<Block> _voidBlocks; //blocks that are "done"

        public int Length { get; set; } //x
        public int Width { get; set; } //z
        public int Height { get; set; } //y


        public BlockArea(int length, int width, int height)
        {
            Width = width;
            Height = height;
            Length = length;

            _bufferStacks = new Stack<Block>[Length, Width];
            _currentPositionOfBlock = new Dictionary<int, Position>();
            InitializeAreas();
            _voidBlocks = new List<Block>();
            ARRIVAL_QUEUE_POSITION = new Position(Length, 0, Width); //adjust arrival stack position to the other corner
            _arrivalQueue = new Queue<Block>();

        }
        public void InitializeAreas()
        {
            for (int i = 0; i < Length; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    _bufferStacks[i, j] = new Stack<Block>();
                }
            }

        }

        public string GetDimensions()
		{
            return $"{Length}x{Width}x{Height}";
		}

		public IEnumerable<Block> GetAllBlocks()
        {
            var blocks = new List<Block>();
            var blocksAsEnumerable = _bufferStacks.Cast<Stack<Block>>().SelectMany(stack => stack);
            blocks.AddRange(blocksAsEnumerable);
            blocks.AddRange(_voidBlocks);
            blocks.AddRange(_arrivalQueue);
            return blocks;
        }

        public IEnumerable<Block> GetRemainingBlocks()
        {
            var blocks = new List<Block>();
            var blocksAsEnumerable = _bufferStacks.Cast<Stack<Block>>().SelectMany(stack => stack);
            blocks.AddRange(blocksAsEnumerable);
            blocks.AddRange(_arrivalQueue);
            return blocks;
        }

        public IEnumerable<Block> GetTopBlocks()
        {
            var stacks = _bufferStacks;
            var topblocks =  from x in Enumerable.Range(0, Length)
                               from z in Enumerable.Range(0, Width)
                               where stacks[x, z].Count > 0
                               select stacks[x, z].Peek();
            if (_arrivalQueue.Count > 0)
                topblocks = topblocks.Append(_arrivalQueue.Peek());
            return topblocks;
        }

        public IEnumerable<Position> GetFreePositions()
        {
            var stacks = _bufferStacks;
            var freeStackPositions = from x in Enumerable.Range(0, Length)
                                     from z in Enumerable.Range(0, Width)
                                     where stacks[x, z].Count < Height
                                     select new Position(x, stacks[x, z].Count, z);

            return freeStackPositions.Append(VOID_POSITION); ;
        }
        public bool IsBlockAccessible(Block block) => IsBlockAccessible(GetPosition(block));
        public IEnumerable<Block> GetBlocksAbove(Block block)
        {
            var position = GetPosition(block);
            var blocksAbove = new List<Block>();

            if (position == VOID_POSITION)
                return blocksAbove;

            if(position == ARRIVAL_QUEUE_POSITION)
            {
                //get all that are infront in the queue of the block
                // Convert queue to array to preserve order
                var queueArray = _arrivalQueue.ToArray();
                // Find the index of our target block
                var blockIndex = Array.IndexOf(queueArray, block);

                if (blockIndex >= 0)
                {
                    // Add all blocks that come after this block in the queue
                    // These are effectively "above" in terms of needing to be moved first
                    for (int i = 0; i < blockIndex; i++)
                    {
                        blocksAbove.Add(queueArray[i]);
                    }
                }
                return blocksAbove; //+1 since the block itself also need to be moved
            }
                

            var stack = _bufferStacks[position.X, position.Z];
            var blocksInStack = stack.Reverse().ToArray();

            for (int i = position.Y + 1; i < blocksInStack.Length; i++)
            {
                blocksAbove.Add(blocksInStack[i]);
            }

            return blocksAbove;
        }
        public bool TryPickupBlockFromPosition(Position position, out Block block)
        {
            block = null;
            if (!IsBlockAccessible(position))
                return false;

            if(position == VOID_POSITION)
            {
                block = _voidBlocks.Last(); //caution may produce unwanted error
                _voidBlocks.Remove(block);
                _currentPositionOfBlock.Remove(block.Id);
                return true;
            }

            if(position == ARRIVAL_QUEUE_POSITION)
            {
                block = _arrivalQueue.Dequeue();
                _currentPositionOfBlock.Remove(block.Id);
                return true;
            }

            block = _bufferStacks[position.X, position.Z].Pop();
            _currentPositionOfBlock.Remove(block.Id);
            return true;
        }

        public bool TryPlaceBlockToPosition(Position position, Block block)
        {
            if (!IsBlockPlaceable(position))
                return false;

            if(position == VOID_POSITION)
            {
                _voidBlocks.Add(block);
                _currentPositionOfBlock[block.Id] = position;
                return true;
            }

            if(position == ARRIVAL_QUEUE_POSITION) //this is needed for the redo operation
            {
                AddToFrontOfArrivalStack(block);
                _currentPositionOfBlock[block.Id] = position;
                return true;
            }   

            _bufferStacks[position.X, position.Z].Push(block);
            _currentPositionOfBlock[block.Id] = position;
            return true;
        }

        public Position GetPosition(Block block)
        {
            if (_currentPositionOfBlock.TryGetValue(block.Id, out var position))
                return position;
            else
                throw new BlockNotFoundException($"Block with Id {block.Id} was not found in the Block Area or is already in the void area");
        }
        public Block GetBlock(int id)
        {
            if (_currentPositionOfBlock.TryGetValue(id, out var position) && position != VOID_POSITION)
                return _bufferStacks[position.X, position.Z].Reverse().ToArray()[position.Y];
            else if (position == VOID_POSITION)
                return _voidBlocks.First(block => block.Id == id);
            else
                throw new BlockNotFoundException($"Block with Id {id} was not found in the Block Area or is already in the void area");
        }

        public bool TryApplyMove(Move move)
        {
            //assert the block source position and block id are valid
            if (_currentPositionOfBlock[move.BlockId] != move.BlockSourcePosition)
                return false;
            if (TryPickupBlockFromPosition(move.BlockSourcePosition, out Block block))
            {
                if (TryPlaceBlockToPosition(move.TargetPosition, block))
                {
                    return true;
                }
                else
                {
                    //if placement fails, put the block back in its original position
                    TryPlaceBlockToPosition(move.BlockSourcePosition, block);
                    return false;
                }
            }
            return false;
        }
        public int GetBlockedBlocksAmount()
        {
            HashSet<Block> blockedBlocks = new HashSet<Block>();

            foreach (var block in this.GetRemainingBlocks())
            {
                // Skip blocks that are in void 
                var position = this.GetPosition(block);
                if (position == BlockArea.VOID_POSITION)
                    continue;
				if (position == ARRIVAL_QUEUE_POSITION)
				{
					blockedBlocks.Add(block);
                    continue;
				}


				// Check if this block needs to go to void
				if (block.TryGetTargetPosition(out Position targetPosition)
                    && targetPosition == BlockArea.VOID_POSITION)
                {
                    // Get blocks above and add them to our count if not already counted
                    foreach (var blockingBlock in this.GetBlocksAbove(block))
                    {
						blockedBlocks.Add(blockingBlock); //if already inside it is ignotred
					}
                }
            }

            return blockedBlocks.Count;
        }

        public int GetTotalPositions()
        {
            return Length * Width * Height;
        }
        public bool AllFinshingCriteriasFullfilled()
        {
            var blocks = GetAllBlocks();
            return blocks.All(block => block.AllFinishingCriteriasSatisfied(this)) && _arrivalQueue.Count == 0;/* && _arrivalStack.Count == 0*/
        }

        public bool IsAreaWithinBounds(Position point1, Position point2)
        {
            int minX = Math.Min(point1.X, point2.X);
            int maxX = Math.Max(point1.X, point2.X);
            int minZ = Math.Min(point1.Z, point2.Z);
            int maxZ = Math.Max(point1.Z, point2.Z);

            return minX >= 0 && maxX < Length &&
                   minZ >= 0 && maxZ < Width;
        }

        public void AddToArrivalStack(Block block)
        {
            _arrivalQueue.Enqueue(block);
            _currentPositionOfBlock[block.Id] = ARRIVAL_QUEUE_POSITION;
        }

        public Block? PeekArrivalStack()
        {
            return _arrivalQueue.Count > 0 ? _arrivalQueue.Peek() : null;
        }

        public int GetArrivalStackCount()
        {
            return _arrivalQueue.Count;
        }

		public string GetStateHash()
		{
			var sb = new StringBuilder();

			// Hash container section stacks by position
			for (int x = 0; x < Length; x++)
			{
				for (int z = 0; z < Width; z++)
				{
					var stack = _bufferStacks[x, z];
					// Always include position coordinates even for empty stacks
					sb.Append($"S{x},{z}:");
					if (stack.Count > 0)
					{
						var blocksInStack = stack.ToArray();
						for (int y = 0; y < blocksInStack.Length; y++)
						{
							sb.Append($"{blocksInStack[y].Id},");
						}
					}
					sb.Append('|'); // Use | as separator to avoid ambiguity
				}
			}

			// Hash void area
			sb.Append("V:");
			if (_voidBlocks.Count > 0)
			{
				foreach (var block in _voidBlocks.OrderBy(b => b.Id))
				{
					sb.Append($"{block.Id},");
				}
			}
			sb.Append('|');

			// Hash arrival stack
			sb.Append("A:");
			if (_arrivalQueue.Count > 0)
			{
				foreach (var block in _arrivalQueue)
				{
					sb.Append($"{block.Id},");
				}
			}

			return sb.ToString();
		}

		public IBlockArea Clone()
        {
            BlockArea clone = new BlockArea(Length, Width, Height);

            //deep copy of _containerSection
            for (int x = 0; x < Length; x++)
            {
                for (int z = 0; z < Width; z++)
                {
                    clone._bufferStacks[x, z] = new Stack<Block>(_bufferStacks[x, z].
                        Select(block => block.Clone()).Reverse());
                }
            }

            //deep copy of _currentPositionOfBlock
            clone._currentPositionOfBlock = new Dictionary<int, Position>(
                _currentPositionOfBlock.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new Position(kvp.Value.X, kvp.Value.Y, kvp.Value.Z)
                )
            );

            //deep copy of _void
            clone._voidBlocks = new List<Block>(_voidBlocks.Select(block => block.Clone()));

            clone._arrivalQueue = new Queue<Block>(_arrivalQueue.Select(block => block.Clone()));

            return clone;
        }

        #region Simulation Helper Methods
        public Position? GenerateRandomInsertPosition()
        {
            RandomGenerator randomGenerator = RandomGenerator.Instance;
            var emptyPositions = GetFreePositions().Except(new List<Position> { BlockArea.VOID_POSITION }).ToList(); //void position is not considered a insert pos
            if (emptyPositions.Count == 0)
                return null;

            return emptyPositions[randomGenerator.Next(emptyPositions.Count)];
        }

        public IEnumerable<int> GetStackHeights()
        {
            for (int x = 0; x < Length; x++)
            {
                for (int z = 0; z < Width; z++)
                {
                    yield return _bufferStacks[x, z].Count;
                }
            }
        }

        public float GetStackUtilization(int x, int z)
        {
            return (float)_bufferStacks[x, z].Count / Height;
        }

        public Dictionary<Position, float> GetPositionAccessibility()
        {
            var accessibility = new Dictionary<Position, float>();

            for (int x = 0; x < Length; x++)
            {
                for (int z = 0; z < Width; z++)
                {
                    var stack = _bufferStacks[x, z];
                    for (int y = 0; y < stack.Count; y++)
                    {
                        var position = new Position(x, y, z);
                        var blocksAbove = stack.Count - y - 1;
                        accessibility[position] = 1.0f / (1 + blocksAbove); // 1 for top blocks, decreasing with depth
                    }
                }
            }

            return accessibility;
        }

        #endregion

        #region helper methods
        private void AddToFrontOfArrivalStack(Block block) //needed for redo
        {
            var currentBlocks = _arrivalQueue.ToArray();
            _arrivalQueue.Clear();
            _arrivalQueue.Enqueue(block);
            foreach (var existingBlock in currentBlocks)
            {
                _arrivalQueue.Enqueue(existingBlock);
            }
        }
        private bool IsBlockAccessible(Position position)
        {
            if (position == VOID_POSITION || position == ARRIVAL_QUEUE_POSITION) //special case
                return true;
            if (!IsValidPosition(position))
                return false;

            int heightOfStack = _bufferStacks[position.X, position.Z].Count;

            return (heightOfStack - 1) == position.Y;
        }

        private bool IsBlockPlaceable(Position position)
        {
            if (position == VOID_POSITION || position == ARRIVAL_QUEUE_POSITION) //special case
                return true;
            if (!IsValidPosition(position))
                return false;

            int heightOfStack = _bufferStacks[position.X, position.Z].Count;

            return heightOfStack < Height && heightOfStack == position.Y;
        }

        private bool IsValidPosition(Position position)
        {
            if (position == VOID_POSITION ||position == ARRIVAL_QUEUE_POSITION) //special case
                return true;
            int sectionAreaLength = Length;
            int sectionAreaWidth = Width;

            bool isInBounds = position.X >= 0 && position.X < sectionAreaLength
                && position.Z >= 0 && position.Z < sectionAreaWidth
                    && position.Y >= 0 && position.Y < Height;

            if (!isInBounds)
                return false;

            return true;
        }

        public IEnumerable<Block> GetBlocksBelow(int BlockId)
        {
            var position = GetPosition(GetBlock(BlockId));
            if (position == VOID_POSITION || position == ARRIVAL_QUEUE_POSITION)
                return Enumerable.Empty<Block>();

            var stack = _bufferStacks[position.X, position.Z];
            var blocksInStack = stack.ToArray();
            var blocksBelow = new List<Block>();

            for (int i = 0; i < position.Y; i++)
            {
                blocksBelow.Add(blocksInStack[i]);
            }

            return blocksBelow;
        }
        #endregion

    }
}
