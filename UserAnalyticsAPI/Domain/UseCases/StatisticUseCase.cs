using Microsoft.EntityFrameworkCore;
using UserAnalyticsAPI.Domain.Data;
using UserAnalyticsAPI.Domain.Data.Models;
using UserAnalyticsAPI.Presentation.DTO.Request;
using UserAnalyticsAPI.Presentation.DTO.Response;

namespace UserAnalyticsAPI.Domain.UseCases;

public class StatisticUseCase
{
    private readonly MainDbContext _mainDbContext;

    public StatisticUseCase (MainDbContext mainDbContext)
    {
        _mainDbContext = mainDbContext;
    }

    public async Task<StatisticResponseDto> ExecuteAsync(StatisticRequestDto dto)
    {
        EventType? eventType = await _mainDbContext.EventTypes
            .Where(et => et.Name == dto.Type)
            .FirstOrDefaultAsync();
        if (eventType is null) {
            throw new ArgumentException($"Event type {dto.Type} not found.");
        }

        var query = _mainDbContext.Events
            .Where(e => e.Timestamp >= dto.From)
            .Where(e => e.Timestamp <= dto.To)
            .Where(e => e.TypeId == eventType.Id);

        var totalEvents = query.CountAsync();
        var uniqueUsers = query.Select(e => e.UserId).Distinct().CountAsync();
        var topPages = query
            .Select(e => e.Metadata["page"].ToString() ?? "")
            .Where(page => page != null)
            .GroupBy(page => page)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        await Task.WhenAll(totalEvents, uniqueUsers, topPages);

        return new StatisticResponseDto
        {
            TotalEvents = await totalEvents,
            UniqueUsers = await uniqueUsers,
            TopPages = await topPages
        };
    }
}
