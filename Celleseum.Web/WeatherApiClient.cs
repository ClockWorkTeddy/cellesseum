using System.Text.Json.Serialization;

namespace Celleseum.Web;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<NumberSet> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        NumberSet numberSet = null;

        numberSet = await httpClient.GetFromJsonAsync<NumberSet>("/turn", cancellationToken);

        return numberSet;
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
