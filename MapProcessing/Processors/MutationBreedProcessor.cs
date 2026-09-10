namespace MapProcessing
{
    /// <summary>
    /// Breeding processor for mutation mode - allows saturation mutations during breeding.
    /// </summary>
    public class MutationBreedProcessor : BreedProcessor
    {
        private readonly Random _random = new Random();
        private readonly byte _maxSaturation;
        private readonly int size;

        public MutationBreedProcessor(int generations, int size)
        {
            _maxSaturation = (byte)Math.Clamp(generations, 1, byte.MaxValue - 1);
            this.size = size;
        }

        public override (byte saturation, sbyte direction) GetMutationValues(Grazer parent)
        {
            byte saturation = parent.Saturation;
            sbyte direction = parent.SaturationDirection;
            var max = (int)Math.Pow(size, 1.2);
            var randomValue = _random.Next(0, max);
            if (randomValue > (max - Math.Pow(_maxSaturation, 1.75) - 1))
            {
                if (saturation >= _maxSaturation)
                {
                    direction = 0;
                }
                else if (saturation == byte.MinValue)
                {
                    direction = 1;
                }

                saturation = (byte)(saturation + direction);
            }

            return (saturation, direction);
        }
    }
}
