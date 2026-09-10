namespace MapProcessing
{
    /// <summary>
    /// Breeding processor for mutation mode - allows saturation mutations during breeding.
    /// </summary>
    public class MutationBreedProcessor : BreedProcessor
    {
        private readonly Random _random = new Random();
        private readonly byte _maxSaturation;

        public MutationBreedProcessor(int generations)
        {
            _maxSaturation = (byte)Math.Clamp(generations, 1, byte.MaxValue - 1);
        }

        public override (byte saturation, sbyte direction) GetMutationValues(Grazer parent)
        {
            byte saturation = parent.Saturation;
            sbyte direction = parent.SaturationDirection;

            var randomValue = _random.Next(0, 100);
            if (randomValue > 90)
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
