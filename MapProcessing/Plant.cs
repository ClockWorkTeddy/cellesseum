using System.Drawing;

namespace MapProcessing
{
    public class Plant : Creature
    {
        public Plant(Point location, Guid guid) : base(location, CellType.Plant, guid)
        {
            Size = DefaultSize;
            LifeSpan = DefaultLifeSpan;
            NutritionValue = DefaultNutritionValue;
            CounsumptionRate = DefaultCounsumptionRate;
            Satiety = DefaultSatiety;
        }
        public override int NutritionValue => 1 * (DefaultNutritionValue + Age / 10);
        public static int DefaultSatiety => 1;
        public static int DefaultNutritionValue => 2;
        public static int DefaultSize => 1;
        public static int DefaultLifeSpan => 1000;
        public static int DefaultCounsumptionRate => 0;
    }
}
