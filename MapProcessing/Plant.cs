using System.Drawing;

namespace MapProcessing
{
    internal class Plant : Creature
    {
        public Plant(Point location, Guid guid) : base(location, CellType.Plant, guid)
        {
            Size = DefaultSize;
            LifeSpan = DefaultLifeSpan;
            NutritionValue = DefaultNutritionValue;
            CounsumptionRate = DefaultCounsumptionRate;
            Satiety = DefaultSatiety;
        }
        public static int DefaultSatiety = 1;
        public static int DefaultNutritionValue = 5;
        public static int DefaultSize = 1;
        public static int DefaultLifeSpan = 100;
        public static int DefaultCounsumptionRate = 0;
    }
}
