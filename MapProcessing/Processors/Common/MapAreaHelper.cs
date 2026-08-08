namespace MapProcessing
{
    internal static class MapAreaHelper
    {
        public static void FillArea(Map map, Creature creature)
        {
            var saturation = creature is Grazer grazer ? grazer.Saturation : (byte)Math.Clamp(creature.NutritionValue, 0, 8);
            var type = creature.Type;
            var baseIndex = creature.Location.Y * map.Width + creature.Location.X;
            var width = map.Width;
            var size = creature.Size;

            for (int y = 0; y < size; y++)
            {
                var rowBase = baseIndex + y * width;
                for (int x = 0; x < size; x++)
                {
                    var index = rowBase + x;
                    map.SetCellType(index, type);
                    map.SetSaturation(index, saturation);
                }
            }
        }

        public static void ClearArea(Map map, Creature creature)
        {
            var baseIndex = creature.Location.Y * map.Width + creature.Location.X;
            var width = map.Width;
            var size = creature.Size;

            for (int y = 0; y < size; y++)
            {
                var rowBase = baseIndex + y * width;
                for (int x = 0; x < size; x++)
                {
                    var index = rowBase + x;
                    map.SetCellType(index, CellType.Empty);
                    map.SetSaturation(index, 0);
                }
            }
        }

        public static bool IsCellFreeFor(Map map, int index, CellType cellType)
        {
            return !((uint)index < (uint)(map.Width * map.Height) && map.GetCellType(index) == cellType);
        }
    }
}
