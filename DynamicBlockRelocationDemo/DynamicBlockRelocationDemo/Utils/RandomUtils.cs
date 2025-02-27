using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBlockRelocationDemo.Utils
{
    //singelton random, for testing/rerunnig purposes
    public sealed class RandomGenerator
    {
        private static readonly Lazy<RandomGenerator> _instance = new Lazy<RandomGenerator>(() => new RandomGenerator());
        private Random _random;

        private RandomGenerator()
        {
            _random = new Random();
        }

        public static RandomGenerator Instance => _instance.Value;

        public void SetSeed(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int min, int max)
        {
            return _random.Next(min, max);
        }
        public int Next(int max)
        {
            return _random.Next(max);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }

    }
}
