using System.Text.Json.Serialization;

namespace Celleseum.Web;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<List<Dictionary<int, int>>> GetWeatherAsync(int size, CancellationToken cancellationToken = default)
    {
        List<Dictionary<int, int>> data = null;

        data = await httpClient.GetFromJsonAsync<List<Dictionary<int,int>>>($"/turn/{size}", cancellationToken);

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
