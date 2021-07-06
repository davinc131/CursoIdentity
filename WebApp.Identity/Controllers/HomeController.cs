using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApp.Identity.Models;

namespace WebApp.Identity.Controllers
{
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<MyUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<MyUser> _userClaimsPrincipalFactory;

    public HomeController(ILogger<HomeController> logger, UserManager<MyUser> userManager, IUserClaimsPrincipalFactory<MyUser> userClaimsPrincipalFactory)
    {
      _logger = logger;
      _userManager = userManager;
      _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
    }

    public IActionResult Index()
    {
      return View();
    }

    public IActionResult Privacy()
    {
      return View();
    }

    [HttpGet]
    public ActionResult Login()
    {
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByNameAsync(model.UserName);
        if(user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
          var principal = await _userClaimsPrincipalFactory.CreateAsync(user);
          await HttpContext.SignInAsync("Identity.Application", principal);
          return RedirectToAction("About");
        }
        ModelState.AddModelError("", "Usuário ou senha inválida!");
      }
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterModel model)
    {
      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByNameAsync(model.UserName);

        if(user == null)
        {
          user = new MyUser()
          {
            Id = Guid.NewGuid().ToString(),
            UserName = model.UserName,
          };
          var result = await _userManager.CreateAsync(user, model.Password);
          Console.WriteLine();
        }

        return View("Success");
      }
      return View();
    }

    [HttpGet]
    public ActionResult Register()
    {
      return View();
    }

    [HttpGet]
    public ActionResult Success()
    {
      return View();
    }

    [HttpGet]
    [Authorize]
    public ActionResult About()
    {
      return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}
