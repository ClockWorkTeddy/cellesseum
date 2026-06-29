namespace MapProcessing
{
    /// <summary>
    /// Abstract base class for game simulations. Orchestrates the execution of processors
    /// in a specific order for each simulation epoch.
    /// </summary>
    public abstract class GameSimulation
    {
        protected readonly BreedProcessor breedProcessor;
        protected readonly InitialPopulationProcessor initialPopulationProcessor = new();
        protected readonly PlantSpawnerProcessor plantSpawner = new();
        protected readonly MoveProcessor moveProcessor = new();
        protected readonly CleanupProcessor cleanupProcessor = new();
        protected readonly AgingProcessor agingProcessor = new();

        protected GameSimulation(BreedProcessor breedProcessor)
        {
            this.breedProcessor = breedProcessor;
        }

        protected virtual bool IncludeGrazerCountsBySaturation => true;

        /// <summary>
        /// Generate simulation frames over the specified number of epochs.
        /// </summary>
        public IEnumerable<AreaData> GenerateFrames(Map map, int term)
        {
            initialPopulationProcessor.Execute(map);

            for (int i = 0; i < term && map.HasGrazers(); i++)
            {
                ExecuteEpoch(map);
                yield return map.SnapShotArea(IncludeGrazerCountsBySaturation);
                map.IncrementEpoch();
            }
        }

        /// <summary>
        /// Execute a single simulation epoch in the correct order:
        /// 1. Spawn plants
        /// 2. Move grazers (and starve)
        /// 3. Breeding
        /// 4. Cleanup
        /// 5. Age creatures
        /// 6. Cleanup again
        /// </summary>
        private void ExecuteEpoch(Map map)
        {
            plantSpawner.Execute(map);
            moveProcessor.Execute(map);
            breedProcessor.Execute(map);
            cleanupProcessor.Execute(map);
            agingProcessor.Execute(map);
            cleanupProcessor.Execute(map);
        }
    }
}
