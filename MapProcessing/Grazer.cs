using System.Drawing;

namespace MapProcessing
{
    internal class Grazer : Creature
    {
        public Grazer(Point location) : base(location, CellType.Grazer)
        {
            Size = DefaultSize;
            LifeSpan = DefaultLifeSpan;
        }
        public static int DefaultSize = 2;
        public static int DefaultLifeSpan = 50;
    }
}
