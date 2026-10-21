using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CampusRent.Server.Controllers;

[ApiController, Route("api/places")]
public class PlacesController(IPlaceSearchService places) : ControllerBase
{
    [HttpGet("suggest"), EnableRateLimiting("places")]
    public async Task<IActionResult> Suggest([FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) return Ok(ApiResponse<object>.Ok(Array.Empty<object>()));
        if (q.Length > 120) return BadRequest(ApiResponse<object>.Fail("Từ khóa vị trí tối đa 120 ký tự."));
        return Ok(ApiResponse<object>.Ok(await places.SuggestAsync(q, cancellationToken)));
    }

    [HttpGet("resolve"), EnableRateLimiting("places")]
    public async Task<IActionResult> Resolve([FromQuery] string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 260) return BadRequest(ApiResponse<object>.Fail("Địa điểm không hợp lệ."));
        var place = await places.ResolveAsync(id, cancellationToken);
        return place is null ? NotFound(ApiResponse<object>.Fail("Không thể xác định vị trí này.")) : Ok(ApiResponse<object>.Ok(place));
    }
}
