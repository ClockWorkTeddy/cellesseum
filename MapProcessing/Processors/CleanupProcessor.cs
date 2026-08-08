namespace MapProcessing
{
    /// <summary>
    /// Processor responsible for cleaning up dead creatures from the map.
    /// </summary>
    public class CleanupProcessor : IProcessor
    {
        public void Execute(Map map)
        {
            var deadCreatures = map.GetDeadCreatures();
            var eatenCreatures = map.GetEatenCreatures();

            foreach (var dc in deadCreatures)
            {
                if (dc is Plant plant)
                {
                    map.RemovePlant(plant.Location.Y * map.Width + plant.Location.X);
                }
                else if (dc is Grazer)
                {
                    map.RemoveGrazer(dc.Id);
                }
                MapAreaHelper.ClearArea(map, dc);
            }

            deadCreatures.Clear();

            foreach (var ec in eatenCreatures)
            {
                map.RemovePlant(ec.Location.Y * map.Width + ec.Location.X);
            }
            eatenCreatures.Clear();
        }

    }
}
