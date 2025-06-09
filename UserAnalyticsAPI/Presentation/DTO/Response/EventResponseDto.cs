using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace UserAnalyticsAPI.Presentation.DTO.Response;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class EventResponseDto
{
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public JObject? Metadata { get; set; }
}
