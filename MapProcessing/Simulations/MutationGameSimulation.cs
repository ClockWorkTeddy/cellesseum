namespace MapProcessing.Simulations
{
    /// <summary>
    /// Mutation game simulation mode - grazers can mutate their saturation during breeding.
    /// </summary>
    public class MutationGameSimulation : GameSimulation
    {
        public MutationGameSimulation(bool smartGrazer) : base(new MutationBreedProcessor(), smartGrazer)
        {
        }
    }
}
