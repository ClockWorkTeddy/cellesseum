using System.Drawing;

namespace MapProcessing
{
    internal class Grazer : Creature
    {
        public Grazer(Point location, Guid guid, byte saturation, sbyte saturationDirection = 1) : base(location, CellType.Grazer, guid)
        {
            Size = DefaultSize;
            LifeSpan = DefaultLifeSpan;
            Speed = DefaultSpeed;
            NutritionValue = DefaultNutritionValue;
            CounsumptionRate = DefaultCounsumptionRate;
            Satiety = DefaultSatiety;
            Saturation = saturation;
            SaturationDirection = saturationDirection;
            BreedingThreshold = (DefaultLifeSpan) * 2;
        }

        public sbyte SaturationDirection { get; set; } = 1;

        public static int DefaultCounsumptionRate => 2;
        public static int DefaultNutritionValue => 2;
        public static int DefaultSize => 2;
        public static int DefaultLifeSpan => 100;
        public static int DefaultSatiety => DefaultLifeSpan / 4;
        public static int DefaultSpeed => 2;

        public int BreedingThreshold;

        public void Eat(Creature plant)
        {
            Satiety += (plant.NutritionValue);
        }
    }
}
