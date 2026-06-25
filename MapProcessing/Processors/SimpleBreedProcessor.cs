namespace MapProcessing
{
    /// <summary>
    /// Breeding processor for simple mode - copies parent saturation without mutations.
    /// </summary>
    public class SimpleBreedProcessor : BreedProcessor
    {
        public override (byte saturation, sbyte direction) GetMutationValues(Grazer parent)
        {
            return (parent.Saturation, parent.SaturationDirection);
        }
    }
}
