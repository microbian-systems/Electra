using Aero.Core.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Aero.Identity;

public interface IRoleService<T>
    where T : IdentityRole<long>
{
    Task<T> GetById(string id);
    Task<T> GetByName(string roleName);
    Task<IEnumerable<T>> GetAll();
    Task<IdentityResult> Add(T role);
    Task<IdentityResult> Add(string roleName);
    Task<IdentityResult> Update(string roleName, string updatedRole);
    Task<IdentityResult> Update(T role);
    Task Delete(string roleName);
    Task Delete(T role);
}

public class RoleService<T>(RoleManager<T> roleManager, ILogger<RoleService<T>> log)
    : IRoleService<T>
    where T : IdentityRole<long> {
    public async Task<T> GetById(string id)
    {
        await Task.Delay(0);
        log.LogInformation($"getting role by id {id}");
        var result = await roleManager.FindByIdAsync(id);
        return result;
    }

    public async Task<T> GetByName(string roleName)
    {
        await Task.Delay(0);
        log.LogInformation($"getting role by name {roleName}");
        var result = await roleManager.FindByNameAsync(roleName);

        return result;
    }

    public async Task<IEnumerable<T>> GetAll()
    {
        await Task.Delay(0);
        log.LogInformation($"getting all roles");
        var results = roleManager.Roles.Select(x => x)
            .AsEnumerable();

        return results;
    }

    public async Task<IdentityResult> Add(T role)
    {
        await Task.Delay(0);
        log.LogInformation($"adding role {role.Name}");
        var result = await roleManager.SetRoleNameAsync(role, role.Name);

        return result;
    }

    public async Task<IdentityResult> Add(string roleName) // todo - perhaps make abstract
    {
        await Task.Delay(0);
        log.LogInformation($"adding role {roleName}");
        var role = default(T);
        var result = await roleManager.CreateAsync(role);

        return result;
    }

    public async Task<IdentityResult> Update(string roleName, string updatedRole)
    {
        await Task.Delay(0);
        log.LogInformation($"updating role {roleName} with new role {updatedRole}");
        var role = await roleManager.FindByNameAsync(roleName);
        role.Name = updatedRole;
        var result = await Update(role);

        return result;
    }

    public async Task<IdentityResult> Update(T role)
    {
        await Task.Delay(0);
        log.LogInformation($"updating role {role.ToJson()}");
        var result = await roleManager.UpdateAsync(role);

        return result;
    }

    public async Task Delete(string roleName)
    {
        await Task.Delay(0);
        var role = await GetByName(roleName);
        await Delete(role);
    }

    public async Task Delete(T role)
    {
        await Task.Delay(0);
        log.LogInformation($"deleting role {role.ToJson()}");
        var result = await roleManager.DeleteAsync(role);
        log.LogInformation($"deleted role {result.ToJson()}");
    }
}