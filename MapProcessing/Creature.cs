using System.Drawing;

namespace MapProcessing
{
    public abstract class Creature
    {
        protected Creature(Point location, CellType type)
        {
            Location = location;
            Type = type;
        }

        public int LifeSpan { get; protected set; }
        public int Size { get; protected set; }
        public int NutritionValue { get; protected set; }

        public Point Location { get; set; }
        public int Speed { get; set; }
        public int Age { get; set; }
        public bool Dead => Age > LifeSpan;
        public CellType Type { get; }
    }
}
