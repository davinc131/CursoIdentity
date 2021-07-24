using AutoMapper;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using WebApi.Domain;

using WebAPI.Identity.DTO;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPI.Identity.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class UserController : ControllerBase
  {
    private readonly IConfiguration _config;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IMapper _mapper;

    public UserController(IConfiguration config, UserManager<User> userManager, SignInManager<User> signInManager, IMapper mapper)
    {
      _config = config;
      _userManager = userManager;
      _signInManager = signInManager;
      _mapper = mapper;
    }

    // GET: api/<UserController>
    [HttpGet]
    public IEnumerable<string> Get()
    {
      return new string[] { "value1", "value2" };
    }

    // GET api/<UserController>/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
      return "value";
    }

    // POST api/<UserController>
    [HttpPost]
    public async Task<IActionResult> Register(UserDto model)
    {
      try
      {
        var user = await _userManager.FindByNameAsync(model.UserName);

        if (user == null)
        {
          user = new User()
          {
            UserName = model.UserName,
            Email = model.UserName
          };
          var result = await _userManager.CreateAsync(user, model.Password);
          if (result.Succeeded)
          {
            var appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == model.UserName.ToUpper());
            var token = GeneranteJWToken(appUser).Result;
            var confirmationEmail = Url.Action("ConfirmEmailAddress", "Home", new { token = token, email = user.Email }, Request.Scheme);
            System.IO.File.WriteAllText("ConfirmEmailAddressLink.txt", confirmationEmail);
            return Ok();
          }
        }
        return Unauthorized();
      }
      catch (Exception ex)
      {
        return this.StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
      }
    }

    // PUT api/<UserController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<UserController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }

    private async Task<string> GeneranteJWToken(User user)
    {
      var Claims = new List<Claim>
      {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName)
      };

      var roles = await _userManager.GetRolesAsync(user);

      foreach (var role in roles)
      {
        Claims.Add(new Claim(ClaimTypes.Role, role));
      }

      var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_config.GetSection("AppSettins:Token").Value));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

      var tokenDescription = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(Claims),
        Expires = DateTime.Now.AddDays(1),
        SigningCredentials = creds
      };

      var tokenHandler = new JwtSecurityTokenHandler();
      var token = tokenHandler.CreateToken(tokenDescription);
      return tokenHandler.WriteToken(token);
    }
  }
}
