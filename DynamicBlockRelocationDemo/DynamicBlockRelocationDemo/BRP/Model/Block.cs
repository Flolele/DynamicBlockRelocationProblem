using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    //currently only supporting 1x1x1 Containers
    public class Block
    {
        public static int _lastId = 0;
        public int Id { get; set; }
        public List<IFinishingCriteria<Block>> FinshingCriteria { get; set; }

        public Block()
        {
            FinshingCriteria = new List<IFinishingCriteria<Block>>();
        }

        public void AddConstraint(IFinishingCriteria<Block> constraint)
        {
            FinshingCriteria.Add(constraint);
        }

        public void AddConstraints(List<IFinishingCriteria<Block>> constraints)
        {
            FinshingCriteria.AddRange(constraints);
        }

        public bool AllFinishingCriteriasSatisfied(BlockArea containerArea)
        {
            return FinshingCriteria.All(x => x.IsSatisfied(this, containerArea));
        }
        public bool TryGetTargetPosition(out Position position)
        {
            position = default;
            BlockTargetPositionConstraint? targetConstraint = FinshingCriteria.OfType<BlockTargetPositionConstraint>().FirstOrDefault();

            if(targetConstraint is not null)
            {
                position = targetConstraint.TargetPosition;
                return true;
            }
            return false;        
            
        }

        public Block Clone()
        {
            Block clonedBlock = new Block { Id = this.Id };
            foreach (var constraint in FinshingCriteria)
            {
                switch (constraint)
                {
                    case BlockTargetPositionConstraint targetConstraint:
                        clonedBlock.AddConstraint(new BlockTargetPositionConstraint(targetConstraint.TargetPosition with { }));
                        break;
                    case ICloneable cloneableConstraint:
                        clonedBlock.AddConstraint((IFinishingCriteria<Block>)cloneableConstraint.Clone());
                        break;
                    default:
                        throw new InvalidOperationException("Cannot clone Constraint");
                }
            }
            return clonedBlock;
        }
    }
}
