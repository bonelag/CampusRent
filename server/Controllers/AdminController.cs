using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

public class AdminController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        // TODO: Thành viên nhóm truyền thống kê và danh sách báo cáo vào View
        return View();
    }
}
