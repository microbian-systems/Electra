using System;
using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Persistence.RavenDB.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Persistence.RavenDB.Extensions;

/// <summary>
/// Extends <see cref="IdentityBuilder"/> so that RavenDB services can be registered through it.
/// </summary>
public static class IdentityBuilderExtensions
{
	/// <summary>
	/// Registers a RavenDB as the user store.
	/// </summary>
	/// <typeparam name="TUser">The type of the user.</typeparam>
	/// <param name="builder">The builder.</param>
	/// <param name="configure">Options configuration callback for identity integration.</param>
	/// <returns></returns>
	public static IdentityBuilder AddRavenDbIdentityStores<TUser>(this IdentityBuilder builder, Action<RavenDbIdentityOptions>? configure = null) 
		where TUser : ElectraUser, new()
	{
		return builder.AddRavenDbIdentityStores<TUser, ElectraRole>(configure);
	}

	/// <summary>
	/// Registers a RavenDB as the user store.
	/// </summary>
	/// <typeparam name="TUser">The type of the user.</typeparam>
	/// <typeparam name="TRole">The type of the role.</typeparam>
	/// <param name="builder">The builder.</param>
	/// <param name="configure">Options configuration callback for identity integration.</param>
	/// <returns>The builder.</returns>
	public static IdentityBuilder AddRavenDbIdentityStores<TUser, TRole>(this IdentityBuilder builder, Action<RavenDbIdentityOptions>? configure = null)
		where TUser : ElectraUser, new()
		where TRole : ElectraRole, new()
	{
		if (configure != null)
		{
			builder.Services.Configure(configure);
		}
		builder.Services.AddScoped<IUserStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IRoleStore<TRole>, RoleStore<TRole>>();
		
		// Advanced stores
		builder.Services.AddScoped<IUserLoginStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserClaimStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserRoleStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserPasswordStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserSecurityStampStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserEmailStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserLockoutStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserTwoFactorStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserPhoneNumberStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserAuthenticatorKeyStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserAuthenticationTokenStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IUserTwoFactorRecoveryCodeStore<TUser>, UserStore<TUser, TRole>>();
		builder.Services.AddScoped<IQueryableUserStore<TUser>, UserStore<TUser, TRole>>();

		return builder;
	}
}