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
        public static int DefaultNutritionValue = 8;
        public static int DefaultSize = 1;
        public static int DefaultLifeSpan = 25;
        public static int DefaultCounsumptionRate = 0;
    }
}
