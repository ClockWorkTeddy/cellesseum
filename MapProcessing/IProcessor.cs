namespace MapProcessing
{
    /// <summary>
    /// Defines the contract for a processor that executes a phase of the simulation.
    /// </summary>
    public interface IProcessor
    {
        /// <summary>
        /// Execute this processor's logic on the given map.
        /// </summary>
        void Execute(Map map);
    }
}
