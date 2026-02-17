using System.Threading.Tasks;
using Aero.EfCore.Data;
using Aero.Models;
using Aero.Models.Entities;
using LanguageExt;

namespace Aero.Persistence.Core;

public interface IUserRepository : IGenericRepository<AeroUser>
{
    Task<Option<AeroUser>> GetFullUserById(string userId);
    Task<Option<AeroUserProfile>> GetUserProfileAsync(string userId);
    Task<Option<UserSettingsModel>> GetUserSettingsAsync(string userId);
}