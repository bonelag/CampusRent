using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Universities;
using CampusRent.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.Controllers;
[ApiController, Route("api/universities")]
public class UniversitiesController(CampusRentDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> GetAll() => Ok(ApiResponse<object>.Ok(await db.Universities.OrderBy(u => u.Name).Select(u => new UniversityDto { Id = u.Id, Name = u.Name, ShortName = u.ShortName, Address = u.Address, Latitude = u.Latitude, Longitude = u.Longitude }).ToListAsync()));
    [HttpPost, Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create(CreateUniversityDto dto)
    {
        if (await db.Universities.AnyAsync(u => u.ShortName == dto.ShortName.Trim().ToUpper())) return Conflict(ApiResponse<object>.Fail("Mã trường đã tồn tại."));
        var university = new University { Name = dto.Name.Trim(), ShortName = dto.ShortName.Trim(), Address = dto.Address, Latitude = dto.Latitude, Longitude = dto.Longitude };
        university.ShortName = university.ShortName.ToUpperInvariant();
        db.Universities.Add(university);
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(ApiResponse<object>.Fail("Mã trường đã tồn tại.")); }
        return Ok(ApiResponse<object>.Ok(new { university.Id, university.Name, university.ShortName, university.Address, university.Latitude, university.Longitude }));
    }
}
