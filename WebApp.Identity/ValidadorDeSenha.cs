using Microsoft.AspNetCore.Identity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Identity
{
  public class ValidadorDeSenha<TUser> : IPasswordValidator<TUser> where TUser : class
  {
    public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
    {
      var userName = await manager.GetUserNameAsync(user);

      if (userName == password)
        return IdentityResult.Failed(
            new IdentityError { Description = "Usuário e senha não podem ser iguáis"}
          );

      if (password.Contains("password"))
        return IdentityResult.Failed(
            new IdentityError { Description = "Senha inválida" }
          );

      if (password.Contains("senha"))
        return IdentityResult.Failed(
            new IdentityError { Description = "senha inválida" }
          );

      return IdentityResult.Success;
    }
  }
}
