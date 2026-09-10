namespace MapProcessing
{
    /// <summary>
    /// Movement processor for mutation mode - only saturation 7 grazers use smart movement.
    /// </summary>
    public class MutationMoveProcessor : MoveProcessor
    {
        public MutationMoveProcessor(bool smartGrazer = false) : base(smartGrazer)
        {
        }

        protected override bool ShouldUseSmartMovement(Creature creature)
        {
            return smartGrazer && creature is Grazer grazer && grazer.Saturation >= 4;
        }
    }
}
