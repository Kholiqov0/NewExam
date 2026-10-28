using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/events")]
public class EventsController(IEventService eventService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EventFilter filter, CancellationToken ct)
        => ToResponse(await eventService.GetPagedAsync(filter, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => ToResponse(await eventService.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Organizer) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> Create(CreateEventDto dto)
        => ToResponse(await eventService.CreateAsync(dto, CurrentUserId));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEventDto dto)
        => ToResponse(await eventService.UpdateAsync(id, dto, CurrentUserId, IsAdmin));
}
