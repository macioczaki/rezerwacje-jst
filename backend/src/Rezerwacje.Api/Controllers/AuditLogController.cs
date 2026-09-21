using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezerwacje.Application.Audit;
using Rezerwacje.Application.Audit.Dtos;

namespace Rezerwacje.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _audit;

    public AuditLogController(IAuditLogService audit)
    {
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAll(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await _audit.GetAllAsync(entityType, entityId, userId, from, to, limit, ct);
        return Ok(result);
    }
}