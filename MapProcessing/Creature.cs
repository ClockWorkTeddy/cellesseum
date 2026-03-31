using System.Drawing;

namespace MapProcessing
{
    public abstract class Creature
    {
        protected Creature(Point location, CellType type, Guid guid)
        {
            Id = guid;
            Location = location;
            Type = type;
        }

        public Guid Id { get; }
        public int LifeSpan { get; protected set; }
        public int Size { get; protected set; }
        public int NutritionValue { get; protected set; }
        public Point Location { get; set; }
        public int Speed { get; set; }
        public int Age { get; set; }
        public bool Dead => Age > LifeSpan || Satiety <= 0;
        protected int CounsumptionRate { get; set; }
        public CellType Type { get; }
        public int Satiety { get; set; }

        public void Starve()
        {
            Satiety -= CounsumptionRate;
        }
    }
}
