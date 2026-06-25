namespace MapProcessing.Simulations
{
    /// <summary>
    /// Simple game simulation mode - grazers breed without saturation mutations.
    /// </summary>
    public class SimpleGameSimulation : GameSimulation
    {
        public SimpleGameSimulation() : base(new SimpleBreedProcessor())
        {
        }
    }
}
