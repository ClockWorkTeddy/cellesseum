namespace MapProcessing
{
    /// <summary>
    /// Breeding processor for mutation mode - allows saturation mutations during breeding.
    /// </summary>
    public class MutationBreedProcessor : BreedProcessor
    {
        private readonly Random _random = new Random();

        public override (byte saturation, sbyte direction) GetMutationValues(Grazer parent)
        {
            byte saturation = parent.Saturation;
            sbyte direction = parent.SaturationDirection;

            var randomValue = _random.Next(0, 100);
            if (randomValue > 96)  // 3% mutation chance
            {
                if (saturation == 7)
                {
                    direction = -1;
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
