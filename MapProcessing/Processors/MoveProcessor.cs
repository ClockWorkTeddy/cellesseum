using System.Drawing;

namespace MapProcessing
{
    /// <summary>
    /// Processor responsible for moving grazers around the map.
    /// </summary>
    public class MoveProcessor : IProcessor
    {
        public void Execute(Map map)
        {
            var grazers = map.GetGrazers();
            var currentGrazers = grazers.Values.ToList();
            foreach (var grazer in currentGrazers)
            {
                grazer.Starve();
                MoveCreature(map, grazer);
            }
        }

        private void MoveCreature(Map map, Creature creature)
        {
            var oldLocation = creature.Location;
            var newLocation = GetNewPositionNearParent(map, creature);

            // If creature didn't move, skip all the work
            if (oldLocation == newLocation)
            {
                if (creature is Grazer grazer)
                {
                    Grazing(map, grazer);
                }
                return;
            }

            // Only clear the old area
            MapAreaHelper.ClearArea(map, creature);

            // Remember where we came from before updating location
            creature.PreviousLocation = oldLocation;

            // Update location and fill new area
            creature.Location = newLocation;
            if (creature is Grazer grazer2)
            {
                Grazing(map, grazer2);
            }
            MapAreaHelper.FillArea(map, creature);
        }

        private void Grazing(Map map, Grazer grazer)
        {
            var plants = map.GetPlants();
            var cellCount = map.Width * map.Height;
            for (int y = 0; y < grazer.Size; y++)
            {
                for (int x = 0; x < grazer.Size; x++)
                {
                    var cellIndex = (grazer.Location.Y + y) * map.Width + grazer.Location.X + x;
                    if ((uint)cellIndex < (uint)cellCount && map.GetCellType(cellIndex) == CellType.Plant)
                    {
                        var eatenPlant = map.GetPlantAt(cellIndex);
                        if (eatenPlant != null)
                        {
                            map.GetEatenCreatures().Add(eatenPlant);
                            grazer.Eat(eatenPlant);
                        }
                    }
                }
            }
        }


        private Point GetNewPositionNearParent(Map map, Creature creature)
        {
            var random = map.GetRandom();
            var maxX = map.Width - creature.Size;
            var maxY = map.Height - creature.Size;
            var chosen = creature.Location;
            var freeCount = 0;

            for (int directionY = -1; directionY <= 1; directionY++)
            {
                for (int directionX = -1; directionX <= 1; directionX++)
                {
                    if (directionX == 0 && directionY == 0)
                    {
                        continue;
                    }

                    var newX = creature.Location.X + directionX * creature.Speed;
                    var newY = creature.Location.Y + directionY * creature.Speed;

                    if ((uint)newX > (uint)maxX || (uint)newY > (uint)maxY)
                    {
                        continue;
                    }

                    // Don't go back to where we just came from
                    if (newX == creature.PreviousLocation.X && newY == creature.PreviousLocation.Y)
                    {
                        continue;
                    }

                    if (!MapAreaHelper.IsCellFreeFor(map, newY * map.Width + newX, CellType.Grazer))
                    {
                        continue;
                    }

                    freeCount++;
                    if (random.Next(freeCount) == 0)
                    {
                        chosen = new Point(newX, newY);
                    }
                }
            }

            return chosen;
        }

    }
}
