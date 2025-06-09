using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UserAnalyticsAPI.Presentation.DTO.Response;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class PagedResponseDto<T>
{
    public int Page { get; set; }
    public int Total { get; set; }
    public int Limit { get; set; }
    public List<T> List { get; set; } = new ();
}
