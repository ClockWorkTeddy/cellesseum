using MapProcessing;
using System.Text.Json.Serialization;

namespace Celleseum.Web;

public class MapClient(HttpClient httpClient)
{
    public async Task<List<AreaData>> GetMap(int width, int height, CancellationToken cancellationToken = default)
    {
        var data = new List<AreaData>();
        await foreach (var frame in GetMapStream(width, height, cancellationToken))
        {
            data.Add(frame);
        }

        return data;
    }

    public async IAsyncEnumerable<AreaData> GetMapStream(int width, int height, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stream = httpClient.GetFromJsonAsAsyncEnumerable<AreaData>($"/turn/{width}/{height}", cancellationToken);
        await foreach (var frame in stream.WithCancellation(cancellationToken))
        {
            if (frame is not null)
            {
                yield return frame;
            }
        }
    }
}

public record NumberSet
{
    [JsonConstructor]
    public NumberSet(int[] numbers)
    {
        Numbers = numbers ?? Array.Empty<int>();
    }

    public int[] Numbers { get; init; }

    public int Average => Numbers.Length > 0 ? Numbers.Sum() / Numbers.Length : 0;
}
