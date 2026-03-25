using MapProcessing;
using System.Text.Json.Serialization;

namespace Celleseum.Web;

public class MapClient(HttpClient httpClient)
{
    public async Task<List<AreaData>> GetMap(int size, CancellationToken cancellationToken = default)
    {

        var data = await httpClient.GetFromJsonAsync<List<AreaData>>($"/turn/{size}", cancellationToken);

        return data;
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
