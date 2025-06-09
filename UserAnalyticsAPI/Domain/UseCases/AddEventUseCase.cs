using Microsoft.EntityFrameworkCore;
using UserAnalyticsAPI.Domain.Data;
using UserAnalyticsAPI.Domain.Data.Models;
using UserAnalyticsAPI.Presentation.DTO.Request;

namespace UserAnalyticsAPI.Domain.UseCases;

public class AddEventUseCase
{
    private readonly MainDbContext _mainDbContext;
    public AddEventUseCase(MainDbContext mainDbContext)
    {
        _mainDbContext = mainDbContext;
    }

    public async Task ExecuteAsync(EventRequestDto dto)
    {
        User? user = await _mainDbContext.Users.Where(u => u.Id == dto.UserId).FirstOrDefaultAsync();
        if (user is null)
        {
            throw new ApplicationException($"User with id {dto.UserId} not found.");
        }

        EventType? eventType = await _mainDbContext.EventTypes.Where(et => et.Name == dto.EventType).FirstOrDefaultAsync();
        if (eventType is null)
        {
            eventType = new EventType()
            {
                Name = dto.EventType
            };

            await _mainDbContext.EventTypes.AddAsync(eventType);
            await _mainDbContext.SaveChangesAsync();
        }

        Event newEvent = new()
        {
            TypeId = eventType.Id,
            UserId = user.Id,
            Metadata = dto.Metadata,
            Timestamp = dto.Timestamp,
        };

        await _mainDbContext.Events.AddAsync(newEvent);
        await _mainDbContext.SaveChangesAsync();
    }
}
