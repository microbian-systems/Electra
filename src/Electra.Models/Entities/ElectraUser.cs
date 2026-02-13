using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using Electra.Core.Entities;
using Electra.Core.Identity;
using Microsoft.AspNetCore.Identity;

namespace Electra.Models.Entities;

public interface IElectraUser : IEntity
{
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
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedOn { get; set; }
    bool IsActive { get; set; }
    string RefreshToken { get; set; }
    DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    DateTimeOffset? LastLoginAt { get; set; }
    ElectraUserProfile Profile { get; set; }
    UserSettingsModel UserSettings { get; set; }
    List<IdentityUserClaim<string>> Claims { get; set; }
    List<IdentityUserLogin<string>> Logins { get; set; }
    List<IdentityUserToken<string>> Tokens { get; set; }
}

public class ElectraUser : ElectraUser<string>, IElectraUser, IEntity;

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
    public ElectraUserProfile Profile { get; set;  }
    public UserSettingsModel UserSettings { get; set; }
    public List<IdentityUserClaim<string>> Claims { get; set; }
    public List<IdentityUserLogin<string>> Logins { get; set; }
    public List<IdentityUserToken<string>> Tokens { get; set; }
    public List<ElectraRole> Roles { get; set; }
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
    public byte[] UserHandle { get; set; } 
    public TKey UserProfileId { get; set; }
    // todo - consider converting the user profile property to a JsonB field vs a Foreign related table
    // Documentation on JsonB columns:
    // https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cpoco
    //[Column(TypeName = "jsonb")]
    [JsonPropertyName("profile")] 
    public virtual ElectraUserProfile Profile { get; set; } = new();
    public virtual UserSettingsModel UserSettings { get; set; } = new();
    public virtual List<IdentityUserClaim<string>> Claims { get; set; } = [];
    public virtual List<IdentityUserLogin<string>> Logins { get; set; } = [];
    public virtual List<IdentityUserToken<string>> Tokens { get; set; } = [];
    public virtual List<ElectraRole> Roles { get; set; } = [];
    [NotMapped]
    [JsonIgnore]
    public virtual List<string> RoleNames
    {
        get { return Roles.Select(x => x.Name).ToList(); }
    }
    public virtual List<string> TwoFactorRecoveryCodes { get; set; } = [];
    public virtual string? TwoFactorAuthenticatorKey { get; set; }
    public virtual List<string> GetRolesList() => RoleNames;
}


