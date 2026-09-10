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
            return !((uint)index < (uint)(map.Width * map.Height) && (int)map.GetCellType(index) >= (int)cellType);
        }

        public static bool IsAreaFreeFor(Map map, int x, int y, int size, CellType cellType)
        {
            if (x < 0 || y < 0 || x + size > map.Width || y + size > map.Height)
            {
                return false;
            }

            for (int offsetY = 0; offsetY < size; offsetY++)
            {
                for (int offsetX = 0; offsetX < size; offsetX++)
                {
                    var index = (y + offsetY) * map.Width + x + offsetX;
                    if (!IsCellFreeFor(map, index, cellType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
