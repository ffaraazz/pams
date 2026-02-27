using PAMS.Domain.Exceptions;

namespace PAMS.Domain.Services;

/// <summary>
/// Circular project-scoped reporting detection (FR-020).
/// Prevents: A leads B, B leads A on the same project.
/// Also blocks self-assignment and duplicate mappings.
/// Detects transitive cycles (A→B, B→C, C→A on same project).
/// </summary>
public sealed class TeamLeadValidator
{
    /// <summary>
    /// Validates that adding a new team lead → reportee mapping does not
    /// create a circular or duplicate assignment on the project.
    /// </summary>
    /// <param name="projectId">The project context.</param>
    /// <param name="teamLeadId">The proposed team lead.</param>
    /// <param name="reporteeId">The proposed reportee.</param>
    /// <param name="existingMappings">
    /// All existing (TeamLeadId, ReporteeId) pairs for this project.
    /// </param>
    /// <exception cref="CircularReportingException">Thrown when circular leadership is detected.</exception>
    /// <exception cref="DomainException">Thrown when a duplicate mapping already exists.</exception>
    public void Validate(
        Guid projectId,
        Guid teamLeadId,
        Guid reporteeId,
        IReadOnlyList<(Guid TeamLeadId, Guid ReporteeId)> existingMappings)
    {
        // Self-assignment check
        if (teamLeadId == reporteeId)
        {
            throw new CircularReportingException(
                "An employee cannot be their own team lead.",
                "ERR_CIRCULAR_TEAM_LEAD");
        }

        // Duplicate check
        foreach (var mapping in existingMappings)
        {
            if (mapping.TeamLeadId == teamLeadId && mapping.ReporteeId == reporteeId)
            {
                throw new DomainException(
                    "This team-lead / reportee assignment already exists for this project.",
                    "ERR_DUPLICATE_TEAM_MEMBER");
            }
        }

        // Circular check: walk from reporteeId as a leader to see if we reach teamLeadId
        // Build adjacency: leader → set of reportees
        if (WouldCreateCycle(teamLeadId, reporteeId, existingMappings))
        {
            throw new CircularReportingException(
                "Circular reporting detected: the reportee is already a Team Lead of the specified team lead on this project.",
                "ERR_CIRCULAR_TEAM_LEAD");
        }
    }

    private static bool WouldCreateCycle(
        Guid teamLeadId,
        Guid reporteeId,
        IReadOnlyList<(Guid TeamLeadId, Guid ReporteeId)> existingMappings)
    {
        // Build a directed graph: leader → reportees
        var graph = new Dictionary<Guid, List<Guid>>();
        foreach (var (leadId, repId) in existingMappings)
        {
            if (!graph.TryGetValue(leadId, out var reportees))
            {
                reportees = [];
                graph[leadId] = reportees;
            }
            reportees.Add(repId);
        }

        // Add the proposed edge: reporteeId → teamLeadId (reverse direction for cycle detection)
        // We need to check: can we reach teamLeadId by following the "reportee leads..." chain?
        // i.e., does reporteeId lead someone who leads someone ... who leads teamLeadId?
        // Actually: the cycle is: teamLeadId leads reporteeId, and reporteeId (transitively) leads teamLeadId

        // Walk: starting from reporteeId as a team-lead, traverse their reportees
        // If we can reach teamLeadId, it's a cycle
        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(reporteeId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            if (graph.TryGetValue(current, out var reportees))
            {
                foreach (var rep in reportees)
                {
                    if (rep == teamLeadId)
                    {
                        return true; // Cycle detected
                    }
                    stack.Push(rep);
                }
            }
        }

        return false;
    }
}
