using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebApp.Identity
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddControllersWithViews();

      var connectionString = @"Integrated Security=SSPI; Persist Security Info=False;User ID=sa;Password=sipef@adm;Initial Catalog=IdentityCurso;Data Source=DESKTOP-6MGSSHL";
      var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
      services.AddDbContext<MyUserDbContext>(
          options => options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationAssembly) )
        );

      services.AddIdentity<MyUser, IdentityRole>(
        options => 
        {
          options.SignIn.RequireConfirmedEmail = true;

          options.Password.RequireDigit = false;
          options.Password.RequireNonAlphanumeric = false;
          options.Password.RequireLowercase = false;
          options.Password.RequireUppercase = false;
          options.Password.RequiredLength = 6;

          options.Lockout.MaxFailedAccessAttempts = 3;
          options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<MyUserDbContext>()
        .AddDefaultTokenProviders()
        .AddPasswordValidator<ValidadorDeSenha<MyUser>>();

      services.AddScoped<IUserClaimsPrincipalFactory<MyUser>, MyUserClaimsPrincipalFactory>();
      services.ConfigureApplicationCookie(options => options.LoginPath = "/Home/Login");
      services.Configure<DataProtectionTokenProviderOptions>(
          options => options.TokenLifespan = TimeSpan.FromHours(3)
        );
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
      }
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Index}/{id?}");
      });
    }
  }
}
