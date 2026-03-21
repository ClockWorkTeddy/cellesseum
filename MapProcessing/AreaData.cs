namespace MapProcessing
{
    public class AreaData
    {
        public int PlantCount { get; set;  } = 0;
        public int GrazerCount { get; set; } = 0;
        public Dictionary<int, int> CurrentArea { get; set; } = new Dictionary<int, int>();
    }
}
