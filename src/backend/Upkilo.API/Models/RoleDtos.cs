using System;
using System.Collections.Generic;

namespace Upkilo.API.Models;

// DTOs for role management (shared between controllers)
public record CreateRoleRequest(
    string Name,
    string? Description = null,
    Dictionary<string, bool>? Permissions = null
);

public record UpdateRoleRequest(
    string? Name = null,
    string? Description = null,
    Dictionary<string, bool>? Permissions = null,
    bool? IsActive = null
);

public record AssignRoleRequest(
    List<Guid> UserIds
);

public class PermissionCategory
{
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, string> Permissions { get; set; } = new();
}
