using System.Diagnostics;
using System.Drawing;

namespace MapProcessing
{
    internal class Grazer : Creature
    {
        public Grazer(Point location, Guid guid) : base(location, CellType.Grazer, guid)
        {
            Size = DefaultSize;
            LifeSpan = DefaultLifeSpan;
            Speed = DefaultSpeed;
            NutritionValue = DefaultNutritionValue;
            CounsumptionRate = DefaultCounsumptionRate;
            Satiety = DefaultSatiety;
        }
        public static int DefaultCounsumptionRate = 1;
        public static int DefaultNutritionValue = 2;
        public static int DefaultSize = 2;
        public static int DefaultLifeSpan = 100;
        public static int DefaultSatiety = DefaultLifeSpan / 4;
        public static int DefaultSpeed = 1;

        public void Eat(Creature plant)
        {
            Satiety += plant.NutritionValue;
            Debug.WriteLine(Satiety);
        }
    }
}
