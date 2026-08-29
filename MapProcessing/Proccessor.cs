using MapProcessing.Simulations;

namespace MapProcessing
{
    /// <summary>
    /// Main processor that orchestrates the simulation.
    /// Supports different game modes through different simulation strategies.
    /// </summary>
    public class Proccessor
    {
        public enum GameMode
        {
            Simple,
            Mutation
        }

        /// <summary>
        /// Process the map simulation as a lazy enumerable (yields frames as generated).
        /// </summary>
        public static IEnumerable<AreaData> ProcessMapFrames(Map map, int term = 3000, GameMode mode = GameMode.Simple, bool smartGrazer = false)
        {
            var simulation = CreateSimulation(mode, smartGrazer);
            return simulation.GenerateFrames(map, term);
        }

        /// <summary>
        /// Create the appropriate simulation strategy for the given game mode.
        /// </summary>
        private static GameSimulation CreateSimulation(GameMode mode, bool smartGrazer)
        {
            return mode switch
            {
                GameMode.Simple => new SimpleGameSimulation(smartGrazer),
                GameMode.Mutation => new MutationGameSimulation(smartGrazer),
                _ => new SimpleGameSimulation(smartGrazer)
            };
        }
    }
}
