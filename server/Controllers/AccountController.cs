using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        // TODO: Thành viên nhóm triển khai xác thực tài khoản và chuyển hướng (RedirectToAction("Index", "Home"))
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string fullName, string email, string password, string phoneNumber, string role)
    {
        // TODO: Thành viên nhóm triển khai lưu tài khoản mới vào CSDL
        return RedirectToAction(nameof(Login));
    }
}
