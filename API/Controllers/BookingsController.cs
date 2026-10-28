using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingDto dto)
        => ToResponse(await bookingService.CreateAsync(dto, CurrentUserId));

    [HttpGet("my")]
    public async Task<IActionResult> GetMy([FromQuery] string? status, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => ToResponse(await bookingService.GetMyAsync(status, page, pageSize, CurrentUserId, ct));

    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> Pay(int id)
        => ToResponse(await bookingService.PayAsync(id, CurrentUserId));

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
        => ToResponse(await bookingService.CancelAsync(id, CurrentUserId, IsAdmin));
}
