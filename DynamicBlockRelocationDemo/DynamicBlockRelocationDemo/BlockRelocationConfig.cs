using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo
{
    public class BlockRelocationConfig
    {
        public int Length { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float MoveCostPerUnit { get; set; }
        public float PickupCost { get; set; }
        public float PlacementCost { get; set; }
        public Position CraneStartPosition { get; set; }
        public Position CraneOperationalAreaStart { get; set; }
        public Position CraneOperationalAreaEnd { get; set; }
        public TimeSpan RuntimeLimit { get; set; }
        public int BeamWidth { get; set; }

        public BlockRelocationConfig()
        {
            // Default values
            Length = 5;
            Width = 5;
            Height = 3;
            MoveCostPerUnit = 1f;
            PickupCost = 5f;
            PlacementCost = 5f;
            CraneStartPosition = new Position(0, 0, 0);
            CraneOperationalAreaStart = new Position(0,0, 0);
            CraneOperationalAreaEnd = new Position(Length - 1, Width - 1, Height - 1);
            RuntimeLimit = TimeSpan.FromSeconds(10);
            BeamWidth = 10;
        }
    }
}
