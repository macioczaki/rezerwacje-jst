using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezerwacje.Application.Reservations;
using Rezerwacje.Application.Reservations.Dtos;

namespace Rezerwacje.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReservationDto>>> GetAll(
        [FromQuery] Guid? roomId,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool onlyActive = true,
        CancellationToken ct = default)
    {
        // Jeśli użytkownik nie jest adminem, może filtrować tylko po sobie.
        var currentUserId = GetCurrentUserId();
        if (!IsAdmin() && userId.HasValue && userId.Value != currentUserId)
            return Forbid();

        var result = await _reservations.GetAllAsync(roomId, userId, from, to, onlyActive, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var reservation = await _reservations.GetByIdAsync(id, ct);

            if (!IsAdmin() && reservation.UserId != GetCurrentUserId())
                return Forbid();

            return Ok(reservation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();
            var reservation = await _reservations.CreateAsync(userId, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, reservation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> Update(
        Guid id,
        [FromBody] UpdateReservationRequest request,
        CancellationToken ct)
    {
        try
        {
            var updated = await _reservations.UpdateAsync(
                id, GetCurrentUserId(), IsAdmin(), request, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            await _reservations.CancelAsync(id, GetCurrentUserId(), IsAdmin(), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("availability")]
    public async Task<ActionResult<AvailabilitySlotDto>> GetAvailability(
        [FromQuery] Guid roomId,
        [FromQuery] DateTime date,
        CancellationToken ct)
    {
        try
        {
            var result = await _reservations.GetAvailabilityAsync(roomId, date, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Brak identyfikatora użytkownika w tokenie.");

        return Guid.Parse(raw);
    }

    private bool IsAdmin() => User.IsInRole("Admin");
}