using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserAnalyticsAPI.Domain.Data;
using UserAnalyticsAPI.Domain.Data.Models;
using UserAnalyticsAPI.Domain.UseCases;
using UserAnalyticsAPI.Presentation.DTO.Request;
using UserAnalyticsAPI.Presentation.DTO.Response;

namespace UserAnalyticsAPI.Presentation.Controllers;

[ApiController]
public class EventController : ControllerBase
{
    private readonly MainDbContext _mainDbContext;
    private readonly AddEventUseCase _addEventUseCase;
    private readonly ListPagedEventsUseCase _listPagedEventsUseCase;
    private readonly StatisticUseCase _statisticUseCase;

    public EventController (
        MainDbContext mainDbContext,
        AddEventUseCase addEventUseCase,
        ListPagedEventsUseCase listPagedEventsUseCase,
        StatisticUseCase statisticUseCase
    )
    {
        _mainDbContext = mainDbContext;
        _addEventUseCase = addEventUseCase;
        _listPagedEventsUseCase = listPagedEventsUseCase;
        _statisticUseCase = statisticUseCase;
    }

    [HttpPost("event")]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(
                ModelState.Select(x => x.Value?.Errors)
                           .Where(y => y?.Count > 0)
                           .ToList()
            );
        }

        try
        {
            await _addEventUseCase.ExecuteAsync(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return NoContent();
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery(Name = "page")]  int page,
        [FromQuery(Name = "count")] int count
    )
    {
        try
        {
            return Ok(await _listPagedEventsUseCase.ExecuteAsync(page, count));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("events")]
    public async Task<IActionResult> DeleteEvents([FromQuery] DateTime before)
    {
        await _mainDbContext.Events.Where(e => e.Timestamp < before).ExecuteDeleteAsync();
        return NoContent();
    }

    [HttpGet("users/{user_id}/events")]
    public async Task<IActionResult> GetEventsByUser(string user_id)
    {
        return Ok(
            await _mainDbContext.Events
            .Where(e => e.UserId == Guid.Parse(user_id))
            .OrderByDescending(e => e.Timestamp)
            .Take(1000)
            .ToListAsync()
        );
    }

    [HttpPost("stats")]
    public async Task<IActionResult> Statistic(
        [FromQuery(Name = "from")] DateTime from,
        [FromQuery(Name = "to")] DateTime to,
        [FromQuery(Name = "type")] string type
    )
    {
        try
        {
            return Ok(
                await _statisticUseCase.ExecuteAsync(new StatisticRequestDto()
                {
                    From = from,
                    To = to,
                    Type = type
                })
            );
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }
}
