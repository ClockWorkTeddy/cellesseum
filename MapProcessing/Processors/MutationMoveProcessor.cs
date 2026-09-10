namespace MapProcessing
{
    /// <summary>
    /// Movement processor for mutation mode - higher saturation grazers use smart movement.
    /// </summary>
    public class MutationMoveProcessor : MoveProcessor
    {
        private readonly byte _smartSaturationThreshold;

        public MutationMoveProcessor(bool smartGrazer = false, int generations = 2) : base(smartGrazer)
        {
            _smartSaturationThreshold = (byte)Math.Max(1, Math.Max(1, generations));
        }

        protected override bool ShouldUseSmartMovement(Creature creature)
        {
            return smartGrazer && creature is Grazer grazer && grazer.Saturation >= _smartSaturationThreshold;
        }
    }
}
