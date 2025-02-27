using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    public interface IBlockArea
    {

        IEnumerable<Block> GetTopBlocks();
        IEnumerable<Position> GetFreePositions();
        IEnumerable<Block> GetRemainingBlocks();
        IEnumerable<Block> GetBlocksBelow(int BlockId);
        bool TryPickupBlockFromPosition(Position position, out Block block);
        bool TryPlaceBlockToPosition(Position position, Block block);
        Position GetPosition(Block block);
        bool TryApplyMove(Move move);
        bool AllFinshingCriteriasFullfilled();
        bool IsAreaWithinBounds(Position topLeft, Position bottomRight);
        bool IsBlockAccessible(Block block);
        public Position? GenerateRandomInsertPosition();
        public Block GetBlock(int blockid);
        IBlockArea Clone();
        IEnumerable<Block> GetBlocksAbove(Block block);

        Block? PeekArrivalStack();
        void AddToArrivalStack(Block block);
        int GetArrivalStackCount();
        string GetStateHash();
        int GetBlockedBlocksAmount();
        IEnumerable<int> GetStackHeights();
        float GetStackUtilization(int x, int z);

        Dictionary<Position, float> GetPositionAccessibility();
        int GetTotalPositions();
		string GetDimensions();
	}

    //public enum EAreaSection
    //{
    //    Incomingarea,
    //    IncomingToBufferHandover,
    //    BufferBays,  //main operating area
    //    BufferToOutgoingHandover,
    //    OutgoingArea
    //}
}
