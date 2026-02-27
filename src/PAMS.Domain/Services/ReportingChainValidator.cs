using PAMS.Domain.Exceptions;

namespace PAMS.Domain.Services;

/// <summary>
/// Circular reporting detection for the organisation chart (FR-007).
/// Walks the reporting chain via a lookup function to detect cycles.
/// Self-reference (reportsTo = self) is also blocked.
/// </summary>
public sealed class ReportingChainValidator
{
    /// <summary>
    /// Validates that setting <paramref name="employeeId"/> to report to
    /// <paramref name="newReportsToId"/> does not create a circular chain.
    /// </summary>
    /// <param name="employeeId">The employee whose reporting line is being changed.</param>
    /// <param name="newReportsToId">The proposed new manager.</param>
    /// <param name="getReportsTo">
    /// Lookup function: given an employee ID, returns who they currently report to (or null).
    /// </param>
    /// <exception cref="CircularReportingException">Thrown when a cycle is detected.</exception>
    public void Validate(Guid employeeId, Guid newReportsToId, Func<Guid, Guid?> getReportsTo)
    {
        // Self-reference check
        if (employeeId == newReportsToId)
        {
            throw new CircularReportingException(
                "An employee cannot report to themselves.");
        }

        // Walk up the chain from newReportsToId looking for employeeId
        var visited = new HashSet<Guid> { employeeId };
        var current = newReportsToId;

        while (true)
        {
            if (current == employeeId)
            {
                throw new CircularReportingException(
                    "Circular reporting chain detected: assigning this manager would create a cycle.");
            }

            if (!visited.Add(current))
            {
                // Already visited but didn't hit employeeId — no cycle involving us
                break;
            }

            var next = getReportsTo(current);
            if (next is null)
            {
                // Reached top of chain — no cycle
                break;
            }

            current = next.Value;
        }
    }

    /// <summary>
    /// Validates a nullable reportsTo assignment.
    /// Null means "no manager" and is always valid.
    /// </summary>
    public void ValidateNullable(Guid employeeId, Guid? newReportsToId, Func<Guid, Guid?> getReportsTo)
    {
        if (newReportsToId is null)
        {
            return;
        }

        Validate(employeeId, newReportsToId.Value, getReportsTo);
    }
}
