using System.Drawing;

namespace MapProcessing
{
    /// <summary>
    /// Processor responsible for spawning plants on the map.
    /// </summary>
    public class PlantSpawnerProcessor : IProcessor
    {
        public void Execute(Map map)
        {
            var random = map.GetRandom();
            var amplifier = 0.025;
            var grazers = map.GetGrazers();
            var plants = map.GetPlants();

            var fertility = (int)(amplifier * (map.Width * map.Height - grazers.Count * Math.Pow(Grazer.DefaultSize, 2) - plants.Count * Math.Pow(Plant.DefaultSize, 2)));

            for (int i = 0; i < fertility; i++)
            {
                var x = 0;
                var y = 0;
                do
                {
                    x = random.Next(0, map.Width);
                    y = random.Next(0, map.Height);

                } while (!MapAreaHelper.IsCellFreeFor(map, y * map.Width + x, CellType.Plant));

                var guid = Guid.NewGuid();
                var plant = new Plant(new Point(x, y), guid);
                map.AddPlant(y * map.Width + x, plant);
                MapAreaHelper.FillArea(map, plant);
            }
        }

    }
}
