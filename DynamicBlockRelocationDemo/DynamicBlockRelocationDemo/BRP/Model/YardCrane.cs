using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.BlockRelocation.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    public class YardCrane : ICrane
    {
        public int Id { get; init; }
        public Position CurrentPosition { get; private set; }
        private (Position Point1, Position Point2)? _operationalArea;

        public YardCrane(int id, Position position)
        {
            Id = id;
            CurrentPosition = position;
        }
        public YardCrane(int id, Position position, (Position point1, Position point2) operationalArea) : this(id, position) 
        {
            _operationalArea = operationalArea;
        }

        public bool IsReachableByCrane(Position position)
        {
            if (!_operationalArea.HasValue)
                return false;

            if (position == BlockArea.VOID_POSITION) //every crane can access void position, TODO change later so its not hard coded
                return true;

            if (position == BlockArea.ARRIVAL_QUEUE_POSITION) //every crane can access arrival stack position, TODO change later so its not hard coded
                return true;

            var (min, max) = GetMinMaxPoints(_operationalArea.Value.Point1, _operationalArea.Value.Point2);

            return position.X >= min.X && position.X <= max.X &&
                   position.Z >= min.Z && position.Z <= max.Z;
        }

        public bool TryMoveTo(Position position)
        {
            if (IsReachableByCrane(position))
            {
                CurrentPosition = position;
                return true;
            }
            return false;
        }

        public void SetOperationalArea(Position point1, Position point2)
        {
            _operationalArea = (point1, point2);
        }

        public ICrane Clone()
        {
            return new YardCrane(
                id: this.Id,
                position: this.CurrentPosition with { },
                operationalArea: this._operationalArea.HasValue
                    ? (this._operationalArea.Value.Point1 with { }, this._operationalArea.Value.Point2 with { })
                    : default
            );
        }

        private (Position Min, Position Max) GetMinMaxPoints(Position p1, Position p2)
        {
            return (
                new Position(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Min(p1.Z, p2.Z)),
                new Position(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y), Math.Max(p1.Z, p2.Z))
            );
        }
    }
}
