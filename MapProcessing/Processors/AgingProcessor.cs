namespace MapProcessing
{
    /// <summary>
    /// Processor responsible for aging creatures and removing nutrition over time.
    /// </summary>
    public class AgingProcessor : IProcessor
    {
        public void Execute(Map map)
        {
            var plants = map.GetPlants();
            var grazers = map.GetGrazers();
            var deadCreatures = map.GetDeadCreatures();

            foreach (var plant in plants)
            {
                OldCreature(map, plant.Value, deadCreatures);
            }
            foreach (var grazer in grazers)
            {
                OldCreatureWithoutSaturation(map, grazer.Value, deadCreatures);
            }
        }

        private void OldCreature(Map map, Creature creature, List<Creature> deadCreatures)
        {
            creature.Age++;

            var baseIndex = creature.Location.Y * map.Width + creature.Location.X;
            map.SetSaturation(baseIndex, (byte)Math.Min(creature.NutritionValue, 8));
            if (creature.Dead)
            {
                deadCreatures.Add(creature);
            }
        }

        private void OldCreatureWithoutSaturation(Map map, Creature creature, List<Creature> deadCreatures)
        {
            creature.Age++;

            var baseIndex = creature.Location.Y * map.Width + creature.Location.X;
            if (creature is Grazer grazer)
            {
                for (int y = 0; y < creature.Size; y++)
                {
                    var rowBase = baseIndex + y * map.Width;
                    for (int x = 0; x < creature.Size; x++)
                    {
                        map.SetSaturation(rowBase + x, grazer.Saturation);
                    }
                }
            }

            if (creature.Dead)
            {
                deadCreatures.Add(creature);
            }
        }
    }
}
