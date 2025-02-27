using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreesearchLib;

namespace DynamicBlockRelocationDemo.BlockRelocation.TreeSearch
{
    public class BlockRelocationProblemState : IMutableState<BlockRelocationProblemState, Move, Minimize>
    {
        private HashSet<string> _visitedStates = new HashSet<string>();// to keep track of all visited states
        public BlockYardManager BlockYardManager { get; init; }
       
        public Stack<Move> AppliedMoves { get; init; }


        private ICostCalculator _costCalculator;
        private Minimize? _cachedBound;

        public BlockRelocationProblemState(BlockYardManager blockYardManager, ICostCalculator costCalculator)
        {
            BlockYardManager = blockYardManager;
            AppliedMoves = new Stack<Move>();
            _costCalculator = costCalculator;
        }

        private BlockRelocationProblemState(BlockRelocationProblemState other)
        {
            BlockYardManager = other.BlockYardManager.Clone();
            AppliedMoves = new Stack<Move>(other.AppliedMoves.Reverse());
            _costCalculator = other._costCalculator; //shared reference
        }

        public bool IsTerminal => AreAllFinshingCriteriasFullfilled();

		public Minimize Bound //=> new Minimize((int)GetTotalMoveCost()); 
		{
			get
			{
				if (!_cachedBound.HasValue)
				{
					int currentCost = (int)GetTotalMoveCost();
					int minRemainingCost = 0;
					HashSet<Block> blocksToMove = new HashSet<Block>();

					//add arrival stack excess penalty for one move
					var excess = BlockYardManager.GetArrivalStackCount() - _costCalculator.IdealArrivalQueueSize; 
					if (excess > 0)
					{
						minRemainingCost += (int)(_costCalculator.ArrivalStackExcessPenalty * excess);
					}

					// First pass: identify blocks that need to move due to being a target or above a target
					foreach (var block in BlockYardManager.GetRemainingBlocks()) //does not include void
					{
						Position currentPos = BlockYardManager.GetPosition(block);
						if (currentPos == BlockArea.ARRIVAL_QUEUE_POSITION)
						{
							//move of arrival stack
							minRemainingCost += (int)(_costCalculator.MinimumPickupCost + _costCalculator.MinmumPlacementCost);
							minRemainingCost += (int)_costCalculator.MoveCostPerUnit; // Move one unit to underestimate
							if (block.TryGetTargetPosition(out Position targetPos))
							{
								if (targetPos == BlockArea.VOID_POSITION)
								{
									//if needs to go to void, will need an intermediate move first
									minRemainingCost += (int)(_costCalculator.MinimumPickupCost + _costCalculator.MinmumPlacementCost);
									minRemainingCost += (int)_costCalculator.MoveCostPerUnit; // Move one unit to underestimate
																							  //then move to void
									minRemainingCost += (int)(_costCalculator.MinimumPickupCost + _costCalculator.MinmumPlacementCost);
									minRemainingCost += (int)_costCalculator.MoveCostPerUnit;
								}
								else
								{
									//if also has target position add a move
									minRemainingCost += (int)(_costCalculator.MinimumPickupCost + _costCalculator.MinmumPlacementCost);
									minRemainingCost += (int)_costCalculator.MoveCostPerUnit; // Move one unit to underestimate
								}
							}
							continue; //dont factor in further
						}

						// If this block needs to go to void
						if (!blocksToMove.Contains(block) && block.TryGetTargetPosition(out Position targetPosition)
							&& targetPosition == BlockArea.VOID_POSITION)
						{
							//calculate move cost for this block to void
							minRemainingCost += (int)(_costCalculator.MinimumPickupCost + _costCalculator.MinmumPlacementCost);
							minRemainingCost += (int)(CalculateManhattanDistance(
								currentPos,
								BlockArea.VOID_POSITION
							) * _costCalculator.MoveCostPerUnit);
                            blocksToMove.Add(block);

							// Add all blocks above it to the move set
							foreach (var blockAbove in BlockYardManager.GetBlocksAbove(block))
							{
								if (!blocksToMove.Contains(blockAbove))
								{
									blocksToMove.Add(blockAbove);

									if (blockAbove.TryGetTargetPosition(out Position blockAboveTarget))
									{
										if (blockAboveTarget == BlockArea.VOID_POSITION)
										{
											// move to void
											minRemainingCost += (int)(_costCalculator.MinimumPickupCost + _costCalculator.MinmumPlacementCost);
											minRemainingCost += (int)(CalculateManhattanDistance(
												BlockYardManager.GetPosition(blockAbove),
												BlockArea.VOID_POSITION
											) * _costCalculator.MoveCostPerUnit);
										}
									}
									else
									{
										// Only needs one temporary move
										minRemainingCost += (int)(_costCalculator.MinimumPickupCost + _costCalculator.MinmumPlacementCost);
										minRemainingCost += (int)_costCalculator.MoveCostPerUnit;
									}
								}
							}
						}
					}

					_cachedBound = new Minimize(currentCost + minRemainingCost);
				}
				return _cachedBound.Value;
			}
		}

		public Minimize? Quality => IsTerminal ? Bound : null;

        public object Clone()
        {
            return new BlockRelocationProblemState(this);
        }

        public void Apply(Move choice)
        {
            _cachedBound = null;
            try
            {
				AppliedMoves.Push(choice);
				BlockYardManager.ApplyMove(choice);
				_visitedStates.Add(BlockYardManager.GetStateHash());
			}
            catch
            {
				AppliedMoves.Pop();
				throw new InvalidOperationException("Move cannot be applied");
            }
        }

        public void UndoLast()
        {
            _cachedBound = null;
            var move = AppliedMoves.Pop();
            //remove hash from visited states
            _visitedStates.Remove(BlockYardManager.GetStateHash());
            BlockYardManager.ApplyMove(move.Reverse());
        }

        public IEnumerable<Move> GetChoices()
        {
            var choices = new List<Move>();
            var currentStateHash = BlockYardManager.GetStateHash();

            foreach (Move move in BlockYardManager.GetAllPossibleMoves())
            {
                //check if state was already visited
                var nextState = (BlockRelocationProblemState)Clone();
                nextState.Apply(move);

                var nextStateHash = nextState.BlockYardManager.GetStateHash();

                // Only add the move if it leads to a new state
                if (!_visitedStates.Contains(nextStateHash))
                {
                    choices.Add(move);
                }

            }



            //debug calculate all the bounds for all moves
            var bounds = new List<(Move, Minimize)>();
            foreach (var choice in choices)
            {
                var state = (BlockRelocationProblemState)Clone();
                state.Apply(choice);
                var bound = state.Bound;
                bounds.Add((choice, bound));
            }

            //Console.WriteLine(bounds.Count);



            return choices;
        }

        private bool AreAllFinshingCriteriasFullfilled()
        {
            return BlockYardManager.AllFinshingCriteriasFullfilled();
        }

        //--------
        private int GetTotalMoveCost()
        {
            return (int)_costCalculator.GetTotalMoveCost(AppliedMoves.Reverse());
        }

        private float GetRemainingMoveCostEstimation()
        {
            float minCost = 0;
            foreach (var block in BlockYardManager.GetRemainingBlocks())
            {
                if (block.TryGetTargetPosition(out Position targetPosition) && targetPosition == BlockArea.VOID_POSITION)
                {
                    Position currentPos = BlockYardManager.GetPosition(block);
                    if (currentPos != BlockArea.VOID_POSITION)
                    {
                        // Minimum cost would be: pickup + direct move to void + placement
                        minCost += _costCalculator.MinimumPickupCost;
                        minCost += CalculateManhattanDistance(currentPos, BlockArea.VOID_POSITION) * _costCalculator.MoveCostPerUnit;
                        minCost += _costCalculator.MinmumPlacementCost;
                    }
                }
            }
            return minCost;
        }

        private float CalculateManhattanDistance(Position from, Position to)
        {
            return (Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y) + Math.Abs(to.Z - from.Z));
        }

        public void Reset()
        {
            AppliedMoves.Clear();
			_visitedStates.Clear();
            _cachedBound = null;
		}
    }
    
}
