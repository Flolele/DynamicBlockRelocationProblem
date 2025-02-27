namespace DynamicBlockRelocationDemo.BlockRelocation.Model
{
    [Serializable]
    public class BlockNotFoundException : Exception
    {
        public BlockNotFoundException()
        {
        }

        public BlockNotFoundException(string? message) : base(message)
        {
        }
    }
}