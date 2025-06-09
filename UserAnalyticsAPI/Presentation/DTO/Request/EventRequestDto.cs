using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace UserAnalyticsAPI.Presentation.DTO.Request;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class EventRequestDto
{
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
