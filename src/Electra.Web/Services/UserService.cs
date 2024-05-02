using System.Threading.Tasks;
using FluentEmail.Core;
using Electra.Common.Web.Extensions;
using Electra.Core.Identity;
using Electra.Models;
using Electra.Services;
using Electra.Services.Geo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Electra.Common.Web.Services;

public interface IAppXUserService : IAppXUserService<AppXUser>{}

public class AppXUserService : AppXUserServiceBase<AppXUser>, IAppXUserService
{
    public AppXUserService(
        SignInManager<AppXUser> signinManager, 
        UserManager<AppXUser> userManager, 
        RoleManager<AppXRole> roleManager, 
        IPasswordService passwordService, 
        IHttpContextAccessor contextAccessor, 
        IFluentEmail fluentEmail, 
        IZipApiService zipService, 
        ILogger<AppXUserService> log) 
        : base(signinManager, userManager, roleManager, passwordService, contextAccessor, fluentEmail, zipService, log)
    {
    }
}

public interface IAppXUserService<T> : IAppXIdentityService<T>
{
    string GetCurrentUserId();

    string GetCurrentUserEmail();

    Task<T> GetCurrentUser();

    Task<bool> ChangePassword(string current, string updated, T user);

    Task<bool> VerifyPassword(string password, T user);
}

public class AppXUserServiceBase<T> : AppXIdentityService<T> where T : AppXUser, new()
{
    protected readonly HttpContext context;

    protected AppXUserServiceBase(
        SignInManager<T> signinManager, 
        UserManager<T> userManager, 
        RoleManager<AppXRole> roleManager, 
        IPasswordService passwordService, 
        IHttpContextAccessor contextAccessor, 
        IFluentEmail fluentEmail, 
        IZipApiService zipService, 
        ILogger<AppXUserServiceBase<T>> log) 
        : base(signinManager, userManager, roleManager, passwordService, contextAccessor, fluentEmail, zipService, log)
    {
        context = contextAccessor.HttpContext;
    }
    
    public string GetCurrentUserId() => context.User.GetUserId();

    public string GetCurrentUserEmail() => context.User.GetUserEmail();
    
    public async Task<T> GetCurrentUser()
    {
        var id = context.User.GetUserId();
        var user = await userManager.FindByIdAsync(id);
        
        return user;
    }

    public async Task<bool> ChangePassword(string current, string updated, T user = null)
    {
        user ??= await GetCurrentUser();
        var res = await base.ChangePassword(user, current, updated);
        
        return res;
    }
    
    public override async Task<bool> VerifyPassword(string password, T user = null)
    {
        user ??= await GetCurrentUser();
        var res = await base.VerifyPassword(password, user);
        return res;
    }
}