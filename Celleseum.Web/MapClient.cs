using MapProcessing;
using System.Text.Json.Serialization;

namespace Celleseum.Web;

public class MapClient(HttpClient httpClient)
{
    public async Task<List<AreaData>> GetMap(int width, int height, string mode = "simple", int terms = 3000, bool smartGrazer = false, CancellationToken cancellationToken = default)
    {
        var data = new List<AreaData>();
        await foreach (var frame in GetMapStream(width, height, mode, terms, smartGrazer, cancellationToken))
        {
            data.Add(frame);
        }

        return data;
    }

    public async IAsyncEnumerable<AreaData> GetMapStream(int width, int height, string mode = "simple", int terms = 3000, bool smartGrazer = false, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var encodedMode = Uri.EscapeDataString(mode);
        var stream = httpClient.GetFromJsonAsAsyncEnumerable<AreaData>($"/turn/{width}/{height}?mode={encodedMode}&terms={terms}&smartGrazer={(smartGrazer ? 1 : 0)}", cancellationToken);
        await foreach (var frame in stream.WithCancellation(cancellationToken))
        {
            if (frame is not null)
            {
                yield return frame;
            }
        }
    }

    public string GetDownloadUrl(int width, int height, string mode = "simple", int terms = 3000, bool smartGrazer = false)
    {
        var encodedMode = Uri.EscapeDataString(mode);
        return $"/turn/{width}/{height}/download?mode={encodedMode}&terms={terms}&smartGrazer={(smartGrazer ? 1 : 0)}";
    }

    public Task<HttpResponseMessage> GetMapDownloadResponse(int width, int height, string mode = "simple", int terms = 3000, bool smartGrazer = false, CancellationToken cancellationToken = default)
    {
        var url = GetDownloadUrl(width, height, mode, terms, smartGrazer);
        return httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
