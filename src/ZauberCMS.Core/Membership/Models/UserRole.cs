using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace ZauberCMS.Core.Membership.Models;

/// <summary>
/// User-Role relationship entity optimized for RavenDB.
/// Uses string IDs for references instead of navigation properties.
/// </summary>
