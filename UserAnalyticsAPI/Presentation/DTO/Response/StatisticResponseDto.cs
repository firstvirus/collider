using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UserAnalyticsAPI.Presentation.DTO.Response;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class StatisticResponseDto
{
    public int TotalEvents { get; set; }
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> TopPages { get; set; } = new Dictionary<string, int>();
}
