using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface IPlanService
{
    Task<Result<IReadOnlyList<PlanDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PlanDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<PlanDto>> CreateAsync(CreatePlanDto dto, CancellationToken ct = default);
    Task<Result<PlanDto>> UpdateAsync(UpdatePlanDto dto, CancellationToken ct = default);
    Task<Result> ArchiveAsync(int id, CancellationToken ct = default);
    Task<Result> RestoreAsync(int id, CancellationToken ct = default);
}
