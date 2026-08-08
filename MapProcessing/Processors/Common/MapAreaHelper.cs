namespace MapProcessing
{
    internal static class MapAreaHelper
    {
        public static void FillArea(Map map, Creature creature)
        {
            var saturation = creature is Grazer grazer ? grazer.Saturation : (byte)Math.Clamp(creature.NutritionValue, 0, 8);
            var type = creature.Type;
            var baseIndex = creature.Location.Y * map.Width + creature.Location.X;

            for (int y = 0; y < creature.Size; y++)
            {
                var rowBase = baseIndex + y * map.Width;
                for (int x = 0; x < creature.Size; x++)
                {
                    map.SetCellType(rowBase + x, type);
                    map.SetSaturation(rowBase + x, saturation);
                }
            }
        }

        public static void ClearArea(Map map, Creature creature)
        {
            var baseIndex = creature.Location.Y * map.Width + creature.Location.X;

            for (int y = 0; y < creature.Size; y++)
            {
                var rowBase = baseIndex + y * map.Width;
                for (int x = 0; x < creature.Size; x++)
                {
                    map.SetCellType(rowBase + x, CellType.Empty);
                    map.SetSaturation(rowBase + x, 0);
                }
            }
        }

        public static bool IsCellFreeFor(Map map, int index, CellType cellType)
        {
            return !((uint)index < (uint)(map.Width * map.Height) && map.GetCellType(index) == cellType);
        }
    }
}
