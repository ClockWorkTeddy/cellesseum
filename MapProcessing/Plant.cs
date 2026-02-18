using System.Drawing;

namespace MapProcessing
{
    internal class Plant : Creature
    {
        public Plant(Point location) : base(location, CellType.Plant)
        {
            Size = DefaultSize;
            LifeSpan = DefaultLifeSpan;
        }
        public static int DefaultSize = 1;
        public static int DefaultLifeSpan = 25;
    }
}
