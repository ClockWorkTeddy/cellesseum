using System.Diagnostics;
using System.Drawing;

namespace MapProcessing
{
    internal class Grazer : Creature
    {
        public Grazer(Point location) : base(location, CellType.Grazer)
        {
            Size = DefaultSize;
            LifeSpan = DefaultLifeSpan;
            Speed = DefaultSpeed;
            NutritionValue = DefaultNutritionValue;
        }

        public static int DefaultNutritionValue = 2;
        public static int DefaultSize = 2;
        public static int DefaultLifeSpan = 50;
        public static int DefaultSpeed = 1;
        public int Satiety = 3;

        public void Eat(Creature plant)
        {
            Satiety += plant.NutritionValue;
            Debug.WriteLine(Satiety);
        }
    }
}
