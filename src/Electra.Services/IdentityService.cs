using Electra.Core;
using Electra.Models.Entities;

namespace Electra.Services;

public interface IElectraIdentityService : IElectraIdentityService<ElectraUser>{}

public interface IElectraIdentityService<T> : IElectraIdentityService<T, string>
    where T : ElectraUser, new()
{ }

public interface IElectraIdentityService<T, TKey>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    Task<UserViewModel> LoginAsync(UserLoginRequest model);
    Task<UserViewModel> LoginAsync(UserLoginRequest model, string password);
    Task<UserViewModel> LoginAsync(string username, string password);
    Task LogoutAsync(UserViewModel model);
    Task LogoutAsync(string username);
    Task<(T user, IdentityResult identityReuslt)> AddUserAsync(T model, string password = "");
    Task<(T user,  IdentityResult identityReuslt)> UpdateUserAsync(T model);
    Task<(T user,  IdentityResult identityReuslt)> DeleteUserAsync(T model);
    Task<(T user,  IdentityResult identityReuslt)> DeleteUserAsync(string id);
    Task<bool> ChangePassword(T user, string current, string updated);
    Task<(bool success, string token, string errorMessage)> GenerateResetPasswordToken(string email);
    Task<(bool success, string token, string[] errors)> ResetPassword(string email, string fromEmail, string url, string subject, string scheme = "https");
    Task<(bool success, string[] errors)> ResetPasswordConfirmation(string email, string token, string password);
    Task<T> GetByIdAsync(string id);
    Task<T> GetByUsernameAsync(string username);
    Task<T> GetByEmailAsync(string email);
    Task<IEnumerable<string>> GetRoles(string userId);
    Task<IdentityResult> AddToRole(T user, string role);
    Task<IdentityResult> AddToRole(string userId, string role);
    Task<IdentityResult> AddToRoles(T user, IEnumerable<string> roles);
    Task<IdentityResult> AddToRoles(string userId, IEnumerable<string> roles);
    Task<IdentityResult> AddClaim(T user, Claim claim);
    Task<IdentityResult> AddClaim(string userId, Claim claim);
    Task<IdentityResult> AddClaimsAsync(T user, IEnumerable<Claim> claims);
    Task<IdentityResult> AddClaimsAsync(string userId, IEnumerable<Claim> claims);
    Task<IDictionary<string,string>> GetClaims(string userId);
    Task<(T model, IdentityResult identityResult)> Register(RegistrationRequestModel model, string createdBy = "User");
    Task<(T model, IdentityResult identityResult)> Register(T user, string password, string createdBy = "User");
    Task<bool> SaveRefreshTokenAsync(string username, string token);
    Task<bool> DeleteRefreshTokenAsync(string username, string refreshToken);
    Task<bool> VerifyPassword(string username, string password);
}

public class ElectraIdentityService : ElectraIdentityService<ElectraUser, string>, IElectraIdentityService
{
    public ElectraIdentityService(
        // SignInManager<ElectraUser> signinManager,
        // UserManager<ElectraUser> userManager,
        // RoleManager<ElectraRole> roleManager,
        // IPasswordService passwordService,
        // IHttpContextAccessor contextAccessor,
        // IFluentEmail fluentEmail,
        // IZipApiService zipService,
        ILogger<ElectraIdentityService> log)
        : base(default, default, default, default, default, default, default, log)
    {
    }

    public override async Task<UserViewModel> LoginAsync(string username, string password)
    {
        var result = await signinManager
            .PasswordSignInAsync(username, password, false, true);

        // return null if user not found
        if (!result.Succeeded)
            return null;

        var identity = userManager.Users.First(u => string.Equals(u.UserName,
            username, StringComparison.InvariantCultureIgnoreCase));

        var roles = await userManager.GetRolesAsync(identity);
        var claims = await userManager.GetClaimsAsync(identity);

        // var jwt = tokenService.GenerateToken(account,
        //     roles?.Select(role => new Claim(ClaimTypes.Role, role)));
        //
        // var refresh = tokenService.GenerateRefreshToken();
        //var res = await SaveRefreshTokenAsync(account.UserName, refresh);

        var user = new UserViewModel()
        {
            Id = identity.Id,
            FirstName = identity.FirstName,
            LastName = identity.LastName,
            Username = identity.UserName ??= string.Empty,
            Email = identity.Email ??= string.Empty
            // Token = jwt,
            // RefreshToken = refresh
        };

        user.Roles.AddRange(roles ?? new List<string>());
        user.Claims.AddRange(claims);

        return user;
    }
}

public abstract class ElectraIdentityService<T>(
    SignInManager<T> signinManager,
    UserManager<T> userManager,
    RoleManager<ElectraRole> roleManager,
    IPasswordService passwordService,
    IHttpContextAccessor contextAccessor,
    IFluentEmail fluentEmail,
    IZipApiService zipService,
    ILogger<ElectraIdentityService<T, string>> log)
    : ElectraIdentityService<T, string>(signinManager, userManager, roleManager, passwordService, contextAccessor,
        fluentEmail, zipService, log)
    where T : ElectraUser, new();

public abstract class ElectraIdentityService<T, TKey> : IElectraIdentityService<T, TKey>
    where T : ElectraUser, new()
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    protected readonly UserManager<T> userManager;
    protected readonly SignInManager<T> signinManager;
    protected readonly RoleManager<ElectraRole> roleManager;
    protected readonly ILogger<ElectraIdentityService<T, TKey>> log;
    protected readonly IPasswordService passwordService;
    protected readonly IFluentEmail fluentEmail;
    protected readonly IZipApiService zipService;

    protected ElectraIdentityService(
        SignInManager<T> signinManager, 
        UserManager<T> userManager,
        RoleManager<ElectraRole> roleManager,
        IPasswordService passwordService,
        IHttpContextAccessor contextAccessor,
        IFluentEmail fluentEmail,
        IZipApiService zipService,
        ILogger<ElectraIdentityService<T, TKey>> log)
    {
        this.log = log;
        this.userManager = userManager;
        this.signinManager = signinManager;
        this.roleManager = roleManager;
        this.passwordService = passwordService;
        this.zipService = zipService;
        this.fluentEmail = fluentEmail;
    }
    
    public async Task<UserViewModel> LoginAsync(UserLoginRequest model)
        => await LoginAsync(model.Username, model.Password);

    // todo - add external auth here as well
    async Task<UserViewModel> IElectraIdentityService<T, TKey>.LoginAsync(UserLoginRequest model)
    {
        throw new NotImplementedException();
    }

    public async Task<UserViewModel> LoginAsync(UserLoginRequest model, string password) =>
        await LoginAsync(model.Username, password);
    
    public async Task LogoutAsync(UserViewModel model)
        => await LogoutAsync(model.Username);
    
    public async Task LogoutAsync(string username)
    {
        // todo - verify this logout code actually works...
        await signinManager.SignOutAsync();
    }

    public abstract Task<UserViewModel> LoginAsync(string username, string password);

    public async Task<(T user, IdentityResult identityReuslt)> AddUserAsync(T model, string password = "")
    {
        if (string.IsNullOrEmpty(model.UserName))
            model.UserName = model.Email;

        if (string.IsNullOrEmpty(password))
            password = passwordService.GeneratePassword();
        
        var res = await userManager.CreateAsync(model, password);
        
        if (!res.Succeeded)
        { 
            log.LogError($"unable to create user {model.ToJson()}");
            log.LogError($"information: {res.Errors.ToJson()}");
            return (null, res);
        }
    
        log.LogInformation($"successfully created user");
            
        return (model, res);
    }

    public async Task<(T user, IdentityResult identityReuslt)> UpdateUserAsync(T model)
    {
        var res = await userManager.UpdateAsync(model);
        
        return (model, res);
    }

    public async Task<(T user, IdentityResult identityReuslt)> DeleteUserAsync(T model)
    {
        var res = await userManager.DeleteAsync(model);
        
        return (model, res);
    }
    

    public async Task<(T user, IdentityResult identityReuslt)> DeleteUserAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        var res = await userManager.DeleteAsync(user);
        
        return (user, res);
    }
    public async Task<bool> ChangePassword(T user, string current, string updated)
    {
        log.LogInformation($"changing password for user: {user.ToJson()}");
        var res = await userManager.ChangePasswordAsync(user, current, updated);
    
        return res.Succeeded;
    }
    public async Task<(bool success, string token, string errorMessage)> GenerateResetPasswordToken(string email)
    {
        var user =  await userManager.FindByEmailAsync(email);
        if (user == null)
            return (false, "", "user not found");
        
        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        return (!string.IsNullOrEmpty(token), token, string.Empty);
    }
    
    public async Task<(bool success, string token, string[] errors)> ResetPassword(string email, string fromEmail, string url, string subject, string scheme = "https")
    {
        log.LogInformation($"generating password reset link for {email}");

        if (string.IsNullOrEmpty(email) || !email.IsValidEmail())
                return (false, string.Empty, new[] {$"email must be in a valid format {email}"});
        
        var passGenRes = await GenerateResetPasswordToken(email);
        if (!passGenRes.success)
            return (false, string.Empty, new []{"email address not found"});

        var token = passGenRes.token;

        var rawUrl = url.Split("?").First();
        url = string.Join(rawUrl, $"?token={token}&email={email}");
        
        log.LogInformation($"generated reset link: {url}");
        
        var template = $@"
                    Click here to reset your email: <a href=""@Model.Url"">Reset your password</a>
                    <br/><br/>
                    <p>If you are having trouble clicking the link above - copy and paste the following URL into your browser:</p>
                    <br/>
                    <p>{HttpUtility.HtmlEncode(url)}</p>
            ";

        var res = await fluentEmail
                .To(email)
                .Subject(subject)
                .UsingTemplate(template, new {Url = url})
                .SendAsync();
        
        if (!res.Successful)
        {
            log.LogError($"sending email failed with error(s): {res.ErrorMessages.ToJson()}");
            return (false, token, res.ErrorMessages.ToArray());
        }
        
        log.LogInformation($"successfully sent password reset email to {email}");


        return (true, token, []);
    }

    public async Task<(bool success, string[] errors)> ResetPasswordConfirmation(string email, string token, string password)
    {
        if (!email.IsValidEmail() || token.IsNullOrEmpty())
            return (false, new[] {$"must have a valid email address and token"});

        var user = await userManager.FindByEmailAsync(email);
        
        if(user == null)
            return (false, new []{$"unable to find user with email {email}"});

        var res = await userManager.ResetPasswordAsync(user, token, password);

        if (!res.Succeeded)
            return (false, res.Errors.Select(x => x.Description).ToArray());
        
        log.LogInformation($"successfully reset password for {email}");

        return (true, []);
    }
    
    public async Task<T> GetByIdAsync(string id) => await userManager.FindByIdAsync(id);


    public async Task<T> GetByUsernameAsync(string username) => await userManager.FindByNameAsync(username);
    
    public async Task<T> GetByEmailAsync(string email) => await userManager.FindByEmailAsync((email));

    public async Task<IEnumerable<string>> GetRoles(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
            return [];

        var roles = await userManager.GetRolesAsync(user);

        return roles;
    }

    public async Task<IdentityResult> AddToRole(T user, string role)
    {
        var result = await userManager.AddToRoleAsync(user, role);
        return result;
    }

    public async Task<IdentityResult> AddToRole(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        return await AddToRole(user, role);
    }

    public async Task<IdentityResult> AddToRoles(T user, IEnumerable<string> roles)
    {
        var result = await userManager.AddToRolesAsync(user, roles);
        return result;
    }

    public async Task<IdentityResult> AddToRoles(string userId, IEnumerable<string> roles)
    {
        var user = await userManager.FindByIdAsync(userId);
        return await AddToRoles(user, roles);
    }

    public async Task<IdentityResult> AddClaim(T user, Claim claim)
    {
        var result = await userManager.AddClaimAsync(user, claim);
        return result;
    }

    // todo - add a add roles and claim by id and byEmail and by Username
    public async Task<IdentityResult> AddClaim(string userId, Claim claim)
    {
        var user = await userManager.FindByIdAsync(userId);
        return await AddClaim(user, claim);
    }

    public async Task<IdentityResult> AddClaimsAsync(T user, IEnumerable<Claim> claims)
    {
        var result = await userManager.AddClaimsAsync(user, claims);
        return result;
    }

    public async Task<IdentityResult> AddClaimsAsync(string userId, IEnumerable<Claim> claims)
    {
        var user = await userManager.FindByIdAsync(userId);
        return await AddClaimsAsync(user, claims);
    }

    public async Task<IDictionary<string,string>> GetClaims(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
            return new Dictionary<string, string>();

        var roles = await userManager.GetClaimsAsync(user);

        var kvps = roles.Any()
            ? roles.Select(x => new KeyValuePair<string,string>(x.Type, x.Value))
            : Array.Empty<KeyValuePair<string,string>>();

        return new Dictionary<string, string>(kvps);
    }
    
    public virtual async Task<(T model, IdentityResult identityResult)> Register(RegistrationRequestModel model, string createdBy = "User")
    {
        var user = RegistrationModelToUser(model, createdBy);

        return await Register(user, model.Password, createdBy);
    }
    
    public virtual async Task<(T model, IdentityResult identityResult)> Register(T user, string password, string createdBy = "User")
    {
        var res = await AddUserAsync(user, password);

        return res;
    }
    

    protected virtual T RegistrationModelToUser(RegistrationRequestModel model, string createdBy = "User") => new()
    {
            Id = Snowflake.NewId().ToString(),
            Email = model.Email,
            FirstName = model.Firstname,
            LastName = model.Lastname,
            UserName = model.Username,
            PhoneNumber = model.MobileNumber,
            CreatedBy = createdBy,
        };

    public async Task<bool> SaveRefreshTokenAsync(string id, string token)
    {
        //throw new NotImplementedException();
        var request = new SaveRefreshTokenRequest(id, token);
        //var success = await saveHandler.ExecuteAsync(request);
        var entity = new RefreshTokens
        {
            Token = token, 
            UserId = id,
            //DateCreated = DateTime.UtcNow,
            //DateModified =  DateTime.UtcNow
        };

        var success = true;
        return await Task.FromResult(success);
    }
    
    public async Task<bool> DeleteRefreshTokenAsync(string username, string refreshToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
        // var request = new SaveRefreshTokenRequest()
        // {
        //     Username = username,
        //     Token = refreshToken
        // };
        //var success = await delHandler.ExecuteAsync(request);
        //return success;
    }

    public virtual async Task<bool> VerifyPassword(string password, T user)
    {
        var res = await userManager.CheckPasswordAsync(user, password);
        return res;
    }
    
    public virtual async Task<bool> VerifyPassword(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);
        var validCredentials = await signinManager.UserManager.CheckPasswordAsync(user, password);

        return validCredentials;
    }
}

public record SaveRefreshTokenRequest(string userId, string token);