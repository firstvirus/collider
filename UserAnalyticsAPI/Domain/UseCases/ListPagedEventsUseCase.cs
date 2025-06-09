using Microsoft.EntityFrameworkCore;
using UserAnalyticsAPI.Domain.Data;
using UserAnalyticsAPI.Domain.Data.Models;
using UserAnalyticsAPI.Presentation.DTO.Response;

namespace UserAnalyticsAPI.Domain.UseCases;

public class ListPagedEventsUseCase
{
    private readonly MainDbContext _mainDbContext;

    public ListPagedEventsUseCase(MainDbContext mainDbContext)
    {
        _mainDbContext = mainDbContext;
    }

    public async Task<PagedResponseDto<Event>> ExecuteAsync(int page, int size)
    {
        List<Event> events = await _mainDbContext.Events
            .AsNoTracking()
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        int total = await _mainDbContext.Events.CountAsync();

        return new ()
        {
            Page = page,
            Limit = size,
            Total = total,
            List = events
        };
    }
}
