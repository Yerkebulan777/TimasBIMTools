using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.Services
{
    public static class RandomUtils
    {
        public static int[] GenerateUniqueNumbers(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentException("Minimal value cannot be bigger than maximal value.");

            Random random = new Random();
            HashSet<int> values = new HashSet<int>(maxValue - minValue + 1);

            for (int i = minValue; i < maxValue;)
            {
                int value = random.Next(minValue, maxValue + 1);

                if (values.Add(value))
                {
                    i += 1;
                }

            }

            return values.ToArray();
        }
    }
}
