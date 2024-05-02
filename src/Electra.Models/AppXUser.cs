using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microbians.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace Microbians.Models
{
    public class AppXUser : IdentityUser, IEntity<string> //, IAuditableEntity
    {
        [PersonalData]
        public DateTime? Birthday { get; set; }
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
        public DateTime RefreshTokenExpiryTime { get; set; }
        
        // todo - consider converting the user profile property to a JsonB field vs a Foreign related table
        // Documentation on JsonB columns: 
        // https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cpoco
        //[Column(TypeName = "jsonb")]
        [JsonPropertyName("profile")]
        public virtual AppXUserProfile Profile { get; } = new();

        public virtual ICollection<IdentityUserClaim<string>> Claims { get; set; } = new List<IdentityUserClaim<string>>();
        public virtual ICollection<IdentityUserLogin<string>> Logins { get; set; } = new List<IdentityUserLogin<string>>();
        public virtual ICollection<IdentityUserToken<string>> Tokens { get; set; } = new List<IdentityUserToken<string>>();
        public virtual ICollection<IdentityUserRole<string>> Roles { get; set; } = new List<IdentityUserRole<string>>();
        // todo - implement identity model properly 
        // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-6.0
    }
}