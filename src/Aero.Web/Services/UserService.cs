using FluentEmail.Core;
using Aero.Common.Web.Extensions;
using Aero.Core.Identity;
using Aero.Models.Entities;
using Aero.Services;
using Aero.Services.Geo;
using Aero.Services.Models;
using Microsoft.AspNetCore.Identity;
using ThrowGuard;

namespace Aero.Common.Web.Services;

public interface IAeroUserService : IAeroUserService<AeroUser>{}

public class AeroUserService : AeroUserServiceBase<AeroUser>, IAeroUserService
{
    public AeroUserService(
        SignInManager<AeroUser> signinManager, 
        UserManager<AeroUser> userManager, 
        RoleManager<AeroRole> roleManager, 
        IPasswordService passwordService, 
        IHttpContextAccessor contextAccessor, 
        IFluentEmail fluentEmail, 
        IZipApiService zipService, 
        ILogger<AeroUserService> log) 
        : base(signinManager, userManager, roleManager, passwordService, contextAccessor, fluentEmail, zipService, log)
    {
    }
}

public interface IAeroUserService<T> : IAeroIdentityService<T>
    where T : AeroUser, new()
{
    string GetCurrentUserId();

    string GetCurrentUserEmail();

    Task<T> GetCurrentUser();

    Task<bool> ChangePassword(string current, string updated, T user);

    Task<bool> VerifyPassword(string password, T user);
}

public class AeroUserServiceBase<T> : AeroIdentityService<T>
    where T : AeroUser, new()
{
    protected readonly HttpContext context;

    protected AeroUserServiceBase(
        SignInManager<T> signinManager,
        UserManager<T> userManager, 
        RoleManager<AeroRole> roleManager, 
        IPasswordService passwordService, 
        IHttpContextAccessor contextAccessor, 
        IFluentEmail fluentEmail, 
        IZipApiService zipService, 
        ILogger<AeroUserServiceBase<T>> log) 
        : base(signinManager, userManager, roleManager, passwordService, contextAccessor, fluentEmail, zipService, log)
    {
        Throw.IfNull(contextAccessor?.HttpContext, nameof(contextAccessor));
        context = contextAccessor?.HttpContext!;
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

    public override async Task<UserViewModel> LoginAsync(string username, string password)
    {
        // todo - implement Login in UserService
        throw new NotImplementedException();
    }

    public override async Task<bool> VerifyPassword(string password, T user = null)
    {
        user ??= await GetCurrentUser();
        var res = await base.VerifyPassword(password, user);
        return res;
    }
}