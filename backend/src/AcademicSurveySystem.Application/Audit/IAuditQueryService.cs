using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Audit;

public interface IAuditQueryService
{
    Task<ApplicationResult<AuditEntriesPageDto>> GetEntriesAsync(
        AuditEntryFilter filter,
        CancellationToken cancellationToken);
}
