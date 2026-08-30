using MessagePack;

namespace MapProcessing
{
    [MessagePackObject]
    public class AreaData
    {
        [Key(0)] public int PlantCount { get; set; } = 0;
        [Key(1)] public int GrazerCount { get; set; } = 0;
        [Key(2)] public int NormalizedScore { get; set; } = 0;
        [Key(3)] public int OverallPlantsCount { get; set; } = 0;
        [Key(4)] public int OverallGrazersCount { get; set; } = 0;
        [Key(5)] public int[]? GrazerCountsBySaturation { get; set; }
        [Key(6)] public byte[] Types { get; set; } = Array.Empty<byte>();
        [Key(7)] public byte[] Saturations { get; set; } = Array.Empty<byte>();
    }
}
