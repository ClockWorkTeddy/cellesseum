using MessagePack;

namespace MapProcessing
{
    [MessagePackObject]
    public sealed class MapExport
    {
        [Key(0)] public int Width { get; init; }
        [Key(1)] public int Height { get; init; }
        [Key(2)] public string Mode { get; init; } = "simple";
        [Key(3)] public int Terms { get; init; }
        [Key(4)] public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        [Key(5)] public List<AreaData> Frames { get; init; } = new();
    }
}
