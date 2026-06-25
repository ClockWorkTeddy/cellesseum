using System.Drawing;

namespace MapProcessing
{
    /// <summary>
    /// Abstract base for breeding processors. Different game modes can implement different breeding strategies.
    /// </summary>
    public abstract class BreedProcessor : IProcessor
    {
        public void Execute(Map map)
        {
            var grazers = map.GetGrazers();
            var currentGrazers = grazers.Values.ToList();
            foreach (var grazer in currentGrazers)
            {
                if (grazer.Satiety > grazer.BreedingThreshold)
                {
                    grazer.Satiety = Grazer.DefaultSatiety;
                    var newLocation = GetNewPositionNearParent(map, grazer);
                    PlaceGrazerWithBreeding(map, newLocation, grazer);
                }
            }
        }

        /// <summary>
        /// Called when a grazer is ready to breed. Subclasses determine mutation behavior.
        /// </summary>
        /// <param name="parent">The parent grazer that will breed</param>
        /// <returns>Saturation value and direction for the offspring</returns>
        public abstract (byte saturation, sbyte direction) GetMutationValues(Grazer parent);

        private void PlaceGrazerWithBreeding(Map map, Point position, Grazer parent)
        {
            var guid = Guid.NewGuid();
            var (saturation, saturationDirection) = GetMutationValues(parent);

            var grazer = new Grazer(position, guid, saturation, saturationDirection);
            map.AddGrazer(guid, grazer);
            Grazing(map, grazer);
            MapAreaHelper.FillArea(map, grazer);
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
