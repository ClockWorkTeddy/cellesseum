namespace MapProcessing
{
    public class AreaData
    {
        public int PlantCount { get; set; } = 0;
        public int GrazerCount { get; set; } = 0;
        public int NormalizedScore { get; set; } = 0;
        public int OverallPlantsCount { get; set; } = 0;
        public int OverallGrazersCount { get; set; } = 0;
        public int[]? GrazerCountsBySaturation { get; set; }
        public byte[] Types { get; set; } = Array.Empty<byte>();
        public byte[] Saturations { get; set; } = Array.Empty<byte>();
    }
}
