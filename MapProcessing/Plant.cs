using System.Drawing;

namespace MapProcessing
{
    internal class Plant : Creature
    {
        public Plant(Point location) : base(location, CellType.Plant) {}
        public override int LifeSpan => 5;
    }
}
