using System.ComponentModel.DataAnnotations.Schema;
using Electra.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Electra.Models;

// public class ElectraUser : ElectraUser<Guid>
// {
// }

public interface IElectraUser
{
    long Id { get; set; }
    string? UserName { get; set; }
    string? NormalizedUserName { get; set; }
    string? Email { get; set; }
    string? NormalizedEmail { get; set; }
    bool EmailConfirmed { get; set; }
    string? PasswordHash { get; set; }
    string? SecurityStamp { get; set; }
    string? ConcurrencyStamp { get; set; }
    string? PhoneNumber { get; set; }
    bool PhoneNumberConfirmed { get; set; }
    bool TwoFactorEnabled { get; set; }
    DateTimeOffset? LockoutEnd { get; set; }
    bool LockoutEnabled { get; set; }
    int AccessFailedCount { get; set; }
    DateTime? Birthday { get; set; }
    string FirstName { get; set; }
    string MiddleName { get; set; }
    string LastName { get; set; }
    string CreatedBy { get; set; }

    // todo - remove data attribute -> ModelBuilding (EF)
    string ProfilePictureDataUrl { get; set; }

    DateTimeOffset CreatedOn { get; set; }
    string ModifiedBy { get; set; }
    DateTimeOffset? ModifiedOn { get; set; }
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedOn { get; set; }
    bool IsActive { get; set; }
    string RefreshToken { get; set; }
    DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    DateTimeOffset? LastLoginAt { get; set; }
    ElectraUserProfile Profile { get; }
    ICollection<IdentityUserClaim<string>> Claims { get; set; }
    ICollection<IdentityUserLogin<string>> Logins { get; set; }
    ICollection<IdentityUserToken<string>> Tokens { get; set; }
    ICollection<IdentityUserRole<string>> Roles { get; set; }
}

public class ElectraUser : ElectraUser<long>, IElectraUser
{
}

public interface IElectraUser<TKey>  where TKey : IEquatable<TKey> 
{
    public DateTime? Birthday { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string CreatedBy { get; set; }

    // todo - remove data attribute -> ModelBuilding (EF)
    public string ProfilePictureDataUrl { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public string ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public bool IsDeleted { get; set; } // todo - make IsDeleted a computed column from DeletedOn == null
    public DateTimeOffset? DeletedOn { get; set; }
    public bool IsActive { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    public ElectraUserProfile Profile { get; }
    public ICollection<IdentityUserClaim<string>> Claims { get; set; }
    public ICollection<IdentityUserLogin<string>> Logins { get; set; }
    public ICollection<IdentityUserToken<string>> Tokens { get; set; }
    public ICollection<IdentityUserRole<string>> Roles { get; set; }
    TKey Id { get; set; }
    string? UserName { get; set; }
    string? NormalizedUserName { get; set; }
    string? Email { get; set; }
    string? NormalizedEmail { get; set; }
    bool EmailConfirmed { get; set; }
    string? PasswordHash { get; set; }
    string? SecurityStamp { get; set; }
    string? ConcurrencyStamp { get; set; }
    string? PhoneNumber { get; set; }
    bool PhoneNumberConfirmed { get; set; }
    bool TwoFactorEnabled { get; set; }
    DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    bool LockoutEnabled { get; set; }
    int AccessFailedCount { get; set; }
    string ToString();
}

public class ElectraUser<TKey> 
    : IdentityUser<TKey>, IEntity<TKey>, IElectraUser<TKey> 
    where TKey : IEquatable<TKey>
{
    [PersonalData] public DateTime? Birthday { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string CreatedBy { get; set; }

    [Column(TypeName = "text")] // todo - remove data attribute -> ModelBuilding (EF)
    public string ProfilePictureDataUrl { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public string ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public bool IsActive { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    
    // todo - consider converting the user profile property to a JsonB field vs a Foreign related table
    // Documentation on JsonB columns:
    // https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cpoco
    //[Column(TypeName = "jsonb")]
    [JsonPropertyName("profile")] public virtual ElectraUserProfile Profile { get; } = new();

    public virtual ICollection<IdentityUserClaim<string>> Claims { get; set; } = new List<IdentityUserClaim<string>>();
    public virtual ICollection<IdentityUserLogin<string>> Logins { get; set; } = new List<IdentityUserLogin<string>>();
    public virtual ICollection<IdentityUserToken<string>> Tokens { get; set; } = new List<IdentityUserToken<string>>();

    public virtual ICollection<IdentityUserRole<string>> Roles { get; set; } = new List<IdentityUserRole<string>>();
    // todo - implement identity model properly
    // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-6.0
}