using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

public class RoomsController : Controller
{
    [HttpGet]
    public IActionResult Index(string? universityId, double? radius, decimal? maxPrice, string? keyword)
    {
        // TODO: Thành viên nhóm truy vấn danh sách phòng từ CSDL và truyền sang View
        return View();
    }

    [HttpGet]
    public IActionResult Details(string id)
    {
        // TODO: Thành viên nhóm lấy chi tiết phòng trọ theo ID và truyền Model vào View
        return View();
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(string title, string address, decimal basePrice, double area, decimal electricPrice, decimal waterPrice, string description)
    {
        // TODO: Thành viên nhóm triển khai lưu phòng trọ mới vào CSDL
        return RedirectToAction(nameof(Index));
    }
}
