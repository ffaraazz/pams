using PAMS.Domain.Entities;

namespace PAMS.Domain.Repositories;

public interface IProjectTeamMemberRepository
{
    Task<IReadOnlyList<ProjectTeamMember>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectTeamMember>> GetByTeamLeadAsync(Guid teamLeadId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default);
    Task<bool> WouldCreateCircularLeadershipAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default);
    Task AddAsync(ProjectTeamMember member, CancellationToken ct = default);
    Task RemoveAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default);
}
