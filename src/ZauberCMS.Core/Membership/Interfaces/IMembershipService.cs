using Electra.Models.Entities;
using ZauberCMS.Core.Membership.Models;
using ZauberCMS.Core.Membership.Parameters;
using ZauberCMS.Core.Shared.Models;

namespace ZauberCMS.Core.Membership.Interfaces;

public interface IMembershipService
{
    Task<ElectraUser?> GetUserAsync(GetUserParameters parameters, CancellationToken cancellationToken = default);
    Task<HandlerResult<ElectraUser>> SaveUserAsync(SaveUserParameters parameters, CancellationToken cancellationToken = default);
    Task<HandlerResult<ElectraUser>> CreateUpdateUserAsync(CreateUpdateUserParameters parameters, CancellationToken cancellationToken = default);
    Task<HandlerResult<ElectraUser>> DeleteUserAsync(DeleteUserParameters parameters, CancellationToken cancellationToken = default);
    Task<PaginatedList<ElectraUser>> QueryUsersAsync(QueryUsersParameters parameters, CancellationToken cancellationToken = default);

    Task<CmsRole?> GetRoleAsync(GetRoleParameters parameters, CancellationToken cancellationToken = default);
    Task<HandlerResult<CmsRole>> SaveRoleAsync(SaveRoleParameters parameters, CancellationToken cancellationToken = default);
    Task<HandlerResult<CmsRole>> DeleteRoleAsync(DeleteRoleParameters parameters, CancellationToken cancellationToken = default);
    Task<PaginatedList<CmsRole>> QueryRolesAsync(QueryRolesParameters parameters, CancellationToken cancellationToken = default);

    Task<AuthenticationResult> LoginUserAsync(LoginUserParameters parameters, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> RegisterUserAsync(RegisterUserParameters parameters, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> ExternalLoginAsync(ExternalLoginParameters parameters, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> ConfirmEmailAsync(ConfirmEmailParameters parameters, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> ForgotPasswordAsync(ForgotPasswordParameters parameters, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> ResetPasswordAsync(ResetPasswordParameters parameters, CancellationToken cancellationToken = default);
    Task<ElectraUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}