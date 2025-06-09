using Microsoft.Extensions.Primitives;

namespace UserAnalyticsAPI.Presentation.DTO.Request;

public class StatisticRequestDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Type { get; set; } = string.Empty;
}
