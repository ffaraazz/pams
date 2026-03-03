using Microsoft.EntityFrameworkCore;
using PAMS.Domain.Entities;
using PAMS.Domain.Repositories;

namespace PAMS.Infrastructure.Persistence.Repositories;

public sealed class ProjectTeamMemberRepository : IProjectTeamMemberRepository
{
    private readonly PamsDbContext _context;

    public ProjectTeamMemberRepository(PamsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProjectTeamMember>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _context.ProjectTeamMembers
            .Where(ptm => ptm.ProjectId == projectId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectTeamMember>> GetByTeamLeadAsync(Guid teamLeadId, CancellationToken ct = default)
        => await _context.ProjectTeamMembers
            .Include(ptm => ptm.Project)
                .ThenInclude(p => p.Account)
            .Include(ptm => ptm.Project)
                .ThenInclude(p => p.Allocations)
            .Where(ptm => ptm.TeamLeadId == teamLeadId)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default)
        => await _context.ProjectTeamMembers.AnyAsync(
            ptm => ptm.ProjectId == projectId && ptm.TeamLeadId == teamLeadId && ptm.ReporteeId == reporteeId, ct);

    public async Task<bool> WouldCreateCircularLeadershipAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default)
    {
        // Load all mappings for this project and check via TeamLeadValidator
        var mappings = await _context.ProjectTeamMembers
            .Where(ptm => ptm.ProjectId == projectId)
            .Select(ptm => new { ptm.TeamLeadId, ptm.ReporteeId })
            .ToListAsync(ct);

        var existingMappings = mappings.Select(m => (m.TeamLeadId, m.ReporteeId)).ToList();

        try
        {
            var validator = new Domain.Services.TeamLeadValidator();
            validator.Validate(projectId, teamLeadId, reporteeId, existingMappings);
            return false;
        }
        catch (Domain.Exceptions.CircularReportingException)
        {
            return true;
        }
    }

    public async Task AddAsync(ProjectTeamMember member, CancellationToken ct = default)
        => await _context.ProjectTeamMembers.AddAsync(member, ct);

    public async Task RemoveAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default)
    {
        var member = await _context.ProjectTeamMembers.FirstOrDefaultAsync(
            ptm => ptm.ProjectId == projectId && ptm.TeamLeadId == teamLeadId && ptm.ReporteeId == reporteeId, ct);

        if (member is not null)
        {
            _context.ProjectTeamMembers.Remove(member);
        }
    }
}
