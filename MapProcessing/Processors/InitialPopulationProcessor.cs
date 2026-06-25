using System.Drawing;

namespace MapProcessing
{
    /// <summary>
    /// Processor responsible for seeding the initial grazer population.
    /// </summary>
    public class InitialPopulationProcessor : IProcessor
    {
        private readonly int _initialGrazerCount;

        public InitialPopulationProcessor(int initialGrazerCount = 1)
        {
            _initialGrazerCount = initialGrazerCount;
        }

        public void Execute(Map map)
        {
            for (int i = 0; i < _initialGrazerCount; i++)
            {
                var position = GetNewPositionRandomly(map);
                PlaceInitialGrazer(map, position);
            }
        }

        private static void PlaceInitialGrazer(Map map, Point position)
        {
            var guid = Guid.NewGuid();
            var grazer = new Grazer(position, guid, 0, 1);

            map.AddGrazer(guid, grazer);
            MapAreaHelper.FillArea(map, grazer);
        }

        private static Point GetNewPositionRandomly(Map map)
        {
            var random = map.GetRandom();
            var x = 0;
            var y = 0;

            do
            {
                x = random.Next(0, map.Width);
                y = random.Next(0, map.Height);
                y = Math.Clamp(y % 2 == 0 ? y : y - 1, 0, map.Height - 1);
                x = Math.Clamp(x % 2 == 0 ? x : x - 1, 0, map.Width - 1);
            } while (!MapAreaHelper.IsCellFreeFor(map, y * map.Width + x, CellType.Grazer));

            return new Point(x, y);
        }

    }
}
