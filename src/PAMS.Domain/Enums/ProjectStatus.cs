namespace PAMS.Domain.Enums;

/// <summary>
/// Lifecycle stage of a project. Manually managed by HR/PM.
/// </summary>
public enum ProjectStatus
{
    /// <summary>Project created but work has not started yet.</summary>
    Upcoming,
    /// <summary>Work is in progress.</summary>
    Active,
    /// <summary>All work finished and project is closed.</summary>
    Completed
}
