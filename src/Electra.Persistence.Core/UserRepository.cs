using System.Threading.Tasks;
using Electra.Models;
using Electra.Models.Entities;
using LanguageExt;

namespace Electra.Persistence.Core;

public interface IUserRepository : IGenericRepository<ElectraUser>
{
    Task<Option<ElectraUser>> GetFullUserById(string userId);
    Task<Option<ElectraUserProfile>> GetUserProfileAsync(string userId);
    Task<Option<UserSettingsModel>> GetUserSettingsAsync(string userId);
}