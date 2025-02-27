using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo
{
    public static class BlockInitializer
    {
		public static string layoutPath = "../../../../input/";
		public static int _lastId = 0;
		public static void InitializeBlocks(BlockArea blockArea, double fillPercentage = 0.8, double targetConstraintProbability = 0.5)
        {
            Random random = new Random();
            int totalPositions = blockArea.Length * blockArea.Width * blockArea.Height;
            int blocksToPlace = (int)(totalPositions * fillPercentage);

            for (int i = 0; i < blocksToPlace; i++)
            {
                Position? insertPosition = blockArea.GenerateRandomInsertPosition();
                if (insertPosition == null)
                {
                    throw new InvalidOperationException("Block Area is full, check ur fill percentage");
                }

                Block block = new Block { Id = ++Block._lastId };
                if (random.NextDouble() < targetConstraintProbability)
                {
                    Position targetPosition = BlockArea.VOID_POSITION;
                    block.AddConstraint(new BlockTargetPositionConstraint(targetPosition));
                }

                blockArea.TryPlaceBlockToPosition(insertPosition, block);
            }
            for (int i = 0; i < 5; i++) //add some blocks to the arrival stack
            {
                Block b = new Block { Id = ++Block._lastId };
                blockArea.AddToArrivalStack(b);
            }

            BlockInitializer.SaveBlockLayoutToFile(blockArea, $"{layoutPath}block_layout.txt");
		}

        public static void SaveBlockLayoutToFile(BlockArea containerArea, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var block in containerArea.GetAllBlocks())
                {
                    Position position = containerArea.GetPosition(block);
                    string targetPosition = "None";
                    if (block.TryGetTargetPosition(out Position target))
                    {
                        targetPosition = $"{target.X},{target.Y},{target.Z}";
                    }
                    writer.WriteLine($"{block.Id},{position.X},{position.Y},{position.Z},{targetPosition}");
                }
            }
        }

		public static void LoadBlockLayoutFromFile(BlockArea containerArea, string filePath)
		{
			string fullPath = layoutPath + filePath;
			try
			{
				// First read all blocks into a list
				List<(int id, Position position, Position? targetPosition)> blocksToPlace = new List<(int, Position, Position?)>();

				using (StreamReader reader = new StreamReader(fullPath))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						string[] parts = line.Split(',');
						int id = int.Parse(parts[0]);
						Position position = new Position(
							int.Parse(parts[1]),
							int.Parse(parts[2]),
							int.Parse(parts[3])
						);

						Position? targetPosition = null;
						if (parts.Length > 4)
						{
							if (parts[4] != "None" && parts.Length >= 7)
							{
								targetPosition = new Position(
									int.Parse(parts[4]),
									int.Parse(parts[5]),
									int.Parse(parts[6])
								);
							}
						}

						blocksToPlace.Add((id, position, targetPosition));
					}
				}

				// Sort by Y coordinate (height) ascending
				blocksToPlace.Sort((a, b) => a.position.Y.CompareTo(b.position.Y));

				// Place blocks in order
				foreach (var (id, position, targetPosition) in blocksToPlace)
				{
					Block block = new Block { Id = id };
					if (targetPosition != null)
					{
						block.AddConstraint(new BlockTargetPositionConstraint(targetPosition));

					}

					if(position == BlockArea.ARRIVAL_QUEUE_POSITION)
					{
						containerArea.AddToArrivalStack(block);
					}
					else if (!containerArea.TryPlaceBlockToPosition(position, block))
					{
						throw new InvalidOperationException($"Failed to place block {id} at position {position}");
					}
					Block._lastId = Math.Max(Block._lastId, id);
				}
			}
			catch (Exception ex)
			{
				throw new IOException($"Failed to load block layout: {ex.Message}", ex);
			}
		}
	}
}
