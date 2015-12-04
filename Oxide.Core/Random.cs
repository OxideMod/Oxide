namespace Oxide.Core
{
    public static class Random
    {
        private static readonly System.Random random;

        static Random()
        {
            random = new System.Random();
        }

        /// <summary>
        /// Returns a random integer  which is bigger than or equal to min and smaller than max. If max equals min, min will be returned.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static int Range(int min, int max) => random.Next(min, max);

        /// <summary>
        /// Returns a random integer which is bigger than or equal to 0 and smaller than max.
        /// </summary>
        /// <param name="max"></param>
        public static int Range(int max) => random.Next(max);

        /// <summary>
        /// Returns a random double between min and max.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static double Range(double min, double max) => min + (random.NextDouble() * (max - min));

        /// <summary>
        /// Returns a random float between min and max.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static float Range(float min, float max) => (float)Range((double)min, (double)max);
    }
}
