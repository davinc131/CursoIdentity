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
        if(user != null && !await _userManager.IsLockedOutAsync(user))
        {
          if(await _userManager.CheckPasswordAsync(user, model.Password))
          {
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
              ModelState.AddModelError("", "Email inváido");
              return View();
            }
            await _userManager.ResetAccessFailedCountAsync(user);

            #region Autenticação em 2 Fatores
            //Bloco que trata a validação em dois fatores.
            //Para ser válido, a autenciação em dois fatores tem que ter sido habilitada pelo usuário
            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
              var validator = await _userManager.GetValidTwoFactorProvidersAsync(user);

              if (validator.Contains("Email"))
              {
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                System.IO.File.WriteAllText("email2sv.txt", token);

                await HttpContext.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, Store2FA(user.Id, "Email"));

                return RedirectToAction("TwoFactor");
              }
            }
            #endregion

            var principal = await _userClaimsPrincipalFactory.CreateAsync(user);
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);
            return RedirectToAction("About");
          }
          await _userManager.AccessFailedAsync(user);
          if(await _userManager.IsLockedOutAsync(user))
          {
            //Enviar alerta para usuário, informando a tentativa de acesso a sua conta.
          }
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
            Email = model.UserName
          };
          var result = await _userManager.CreateAsync(user, model.Password);
          if (result.Succeeded)
          {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationEmail = Url.Action("ConfirmEmailAddress", "Home", new { token = token, email = user.Email }, Request.Scheme);
            System.IO.File.WriteAllText("ConfirmEmailAddressLink.txt", confirmationEmail);
            return View("Success");
          }
          else
          {
            foreach (var erro in result.Errors)
            {
              ModelState.AddModelError("", erro.Description);
            }
            return View();
          }
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
    public async Task<IActionResult> ConfirmEmailAddress(string token, string email)
    {
      var user = await _userManager.FindByEmailAsync(email);


      if(user != null)
      {
        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
          return View("Success");
      }

      return View("Error");
    }

    [HttpGet]
    public ActionResult ForgotPassword()
    {
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
    {
      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if(user != null)
        {
          var token = await _userManager.GeneratePasswordResetTokenAsync(user);
          var resetUrl = Url.Action("ResetPassword", "Home", new { token = token, email = model.Email}, Request.Scheme);
          System.IO.File.WriteAllText("ResetLink.txt", resetUrl);
          return View("Success");
        }
        else
        {
          //Fazer alguma coisa, caso o usuário não seja encontrado
        }
      }
      return View();
    }

    [HttpGet]
    public ActionResult ResetPassword(string token, string email)
    {
      return View(new ResetPasswordModel { Token = token, Email = email});
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
    {
      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if(user != null)
        {
          var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

          if (!result.Succeeded)
          {
            foreach (var item in result.Errors)
            {
              ModelState.AddModelError("Erro: ", item.Description);
            }
            return View();
          }
          return View("Success");
        }
      }
      return View();
    }

    [HttpGet]
    public ActionResult Success()
    {
      return View();
    }

    [HttpGet]
    public ActionResult TwoFactor()
    {
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> TwoFactor(TwoFactorModel model)
    {
      var result = await HttpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
      if (!result.Succeeded)
      {
        ModelState.AddModelError("", "Seu token expirou!");
        return View();
      }

      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByIdAsync(result.Principal.FindFirstValue("sub"));
        if (user != null)
        {
          var isValid = await _userManager.VerifyTwoFactorTokenAsync(
              user,
              result.Principal.FindFirstValue("amr"), model.Token);

          if (isValid)
          {
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

            var claimsPrincipal = await _userClaimsPrincipalFactory.CreateAsync(user);
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, claimsPrincipal);

            return RedirectToAction("About");
          }

          ModelState.AddModelError("", "Invalid Token");
          return View();
        }

        ModelState.AddModelError("", "Invalid Request");
      }

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

    private ClaimsPrincipal Store2FA(string UserId, string Provider)
    {
      var identity = new ClaimsIdentity(new List<Claim> { 
        new Claim("sub", UserId),
        new Claim("amr", Provider)
      }, IdentityConstants.ApplicationScheme);

      return new ClaimsPrincipal(identity);
    }
  }
}
