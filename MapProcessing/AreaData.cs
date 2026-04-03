using System.Drawing;

namespace MapProcessing
{
    public class AreaData
    {
        public int PlantCount { get; set;  } = 0;
        public int GrazerCount { get; set; } = 0;
        public Dictionary<int, Point> CurrentArea { get; set; } = new Dictionary<int, Point>();
    }
}
