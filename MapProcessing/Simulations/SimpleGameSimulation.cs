namespace MapProcessing.Simulations
{
    /// <summary>
    /// Simple game simulation mode - grazers breed without saturation mutations.
    /// </summary>
    public class SimpleGameSimulation : GameSimulation
    {
        protected override bool IncludeGrazerCountsBySaturation => false;

        public SimpleGameSimulation(bool smartGrazer) : base(new SimpleBreedProcessor(), smartGrazer)
        {
        }
    }
}
