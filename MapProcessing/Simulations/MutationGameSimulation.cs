namespace MapProcessing.Simulations
{
    /// <summary>
    /// Mutation game simulation mode - grazers can mutate their saturation during breeding.
    /// </summary>
    public class MutationGameSimulation : GameSimulation
    {
        private readonly int _grazerSaturationLevelCount;

        protected override int GrazerSaturationLevelCount => _grazerSaturationLevelCount;

        public MutationGameSimulation(bool smartGrazer, int generations) : base(new MutationBreedProcessor(generations), new MutationMoveProcessor(smartGrazer, generations))
        {
            _grazerSaturationLevelCount = Math.Max(1, generations + 1);
        }
    }
}
