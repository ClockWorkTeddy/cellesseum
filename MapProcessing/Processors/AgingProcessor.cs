using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MapProcessing
{
    /// <summary>
    /// Processor responsible for aging creatures and removing nutrition over time.
    /// </summary>
    public class AgingProcessor : IProcessor
    {
        // Threshold for when parallel processing becomes beneficial (based on cell count)
        private const int ParallelThreshold = 100000; // ~316x316 map

        public void Execute(Map map)
        {
            var plants = map.GetPlants();
            var grazers = map.GetGrazers();
            var deadCreatures = map.GetDeadCreatures();
            var cellCount = map.Width * map.Height;

            // Use parallel processing for large maps where it provides benefit
            if (cellCount >= ParallelThreshold)
            {
                var localDead = new ConcurrentBag<Creature>();

                Parallel.ForEach(plants.Values, plant =>
                    OldCreature(map, plant, localDead));

                Parallel.ForEach(grazers.Values, grazer =>
                    OldCreatureWithoutSaturation(map, grazer, localDead));

                foreach (var dead in localDead)
                    deadCreatures.Add(dead);
            }
            else
            {
                // Sequential processing for small maps is faster than parallel overhead
                foreach (var plant in plants.Values)
                    OldCreature(map, plant, deadCreatures);

                foreach (var grazer in grazers.Values)
                    OldCreatureWithoutSaturation(map, grazer, deadCreatures);
            }
        }

        private static void OldCreature(Map map, Creature creature, IProducerConsumerCollection<Creature> deadCreatures)
        {
            creature.Age++;

            var baseIndex = creature.Location.Y * map.Width + creature.Location.X;
            map.SetSaturation(baseIndex, (byte)Math.Min(creature.NutritionValue, 8));
            if (creature.Dead)
            {
                deadCreatures.TryAdd(creature);
            }
        }

        private static void OldCreature(Map map, Creature creature, List<Creature> deadCreatures)
        {
            creature.Age++;

            var baseIndex = creature.Location.Y * map.Width + creature.Location.X;
            map.SetSaturation(baseIndex, (byte)Math.Min(creature.NutritionValue, 8));
            if (creature.Dead)
            {
                deadCreatures.Add(creature);
            }
        }

        private static void OldCreatureWithoutSaturation(Map map, Creature creature, IProducerConsumerCollection<Creature> deadCreatures)
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
                deadCreatures.TryAdd(creature);
            }
        }

        private static void OldCreatureWithoutSaturation(Map map, Creature creature, List<Creature> deadCreatures)
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
