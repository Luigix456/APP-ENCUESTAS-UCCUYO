using AcademicSurveySystem.Application.Academic.AcademicUnits;
using AcademicSurveySystem.Application.Academic.Attention;
using AcademicSurveySystem.Application.Academic.AcademicCycles;
using AcademicSurveySystem.Application.Academic.Careers;
using AcademicSurveySystem.Application.Academic.Common;
using AcademicSurveySystem.Application.Academic.Subjects;
using AcademicSurveySystem.Application.Academic.SubjectEnrollments;
using AcademicSurveySystem.Application.Academic.Teachers;
using AcademicSurveySystem.Application.Academic.TeacherSubjectAssignments;
using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic;

public interface IAcademicCatalogService
{
    Task<ApplicationResult<IReadOnlyCollection<AcademicUnitDto>>> GetAcademicUnitsAsync(
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<ApplicationResult<AcademicUnitDto>> GetAcademicUnitByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<AcademicUnitDto>> CreateAcademicUnitAsync(
        CreateAcademicUnitRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateAcademicUnitAsync(
        Guid id,
        UpdateAcademicUnitRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateAcademicUnitAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateAcademicUnitAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<CareerDto>>> GetCareersAsync(
        bool includeInactive,
        Guid? academicUnitId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CareerDto>> GetCareerByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CareerDto>> CreateCareerAsync(
        CreateCareerRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateCareerAsync(
        Guid id,
        UpdateCareerRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateCareerAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateCareerAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<TeacherDto>>> GetCareerTeachersAsync(
        Guid careerId,
        Guid? academicCycleId,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<ApplicationResult<AcademicAttentionDto>> GetCareerAttentionAsync(
        Guid careerId,
        Guid academicCycleId,
        AcademicAttentionPermissions permissions,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<AcademicCycleDto>>> GetAcademicCyclesAsync(
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<ApplicationResult<AcademicCycleDto>> GetAcademicCycleByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<AcademicCycleDto>> CreateAcademicCycleAsync(
        CreateAcademicCycleRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateAcademicCycleAsync(
        Guid id,
        UpdateAcademicCycleRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateAcademicCycleAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateAcademicCycleAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<SubjectDto>>> GetSubjectsAsync(
        bool includeInactive,
        Guid? careerId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubjectDto>> GetSubjectByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubjectDto>> CreateSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateSubjectAsync(
        Guid id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateSubjectAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateSubjectAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<SubjectEnrollmentDto>>> GetSubjectEnrollmentsAsync(
        Guid? subjectId,
        Guid? academicCycleId,
        Guid? careerId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubjectEnrollmentDto>> GetSubjectEnrollmentAsync(
        Guid subjectId,
        Guid academicCycleId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubjectEnrollmentDto>> SetSubjectEnrollmentAsync(
        Guid subjectId,
        Guid academicCycleId,
        SetSubjectEnrollmentRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubjectEnrollmentImportTemplateFileDto>> GenerateSubjectEnrollmentImportTemplateAsync(
        Guid careerId,
        Guid academicCycleId,
        string? format,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubjectEnrollmentImportPreviewDto>> PreviewSubjectEnrollmentImportAsync(
        Guid careerId,
        Guid academicCycleId,
        SubjectEnrollmentImportFile file,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SubjectEnrollmentImportResultDto>> ImportSubjectEnrollmentsAsync(
        Guid careerId,
        Guid academicCycleId,
        SubjectEnrollmentImportFile file,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<TeacherDto>>> GetTeachersAsync(
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TeacherDto>> GetTeacherByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TeacherDto>> CreateTeacherAsync(
        CreateTeacherRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateTeacherAsync(
        Guid id,
        UpdateTeacherRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateTeacherAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateTeacherAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<TeacherSubjectAssignmentDto>>> GetTeacherSubjectAssignmentsAsync(
        bool includeInactive,
        Guid? careerId,
        Guid? teacherId,
        Guid? subjectId,
        Guid? academicCycleId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TeacherSubjectAssignmentDto>> GetTeacherSubjectAssignmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult<TeacherSubjectAssignmentDto>> CreateTeacherSubjectAssignmentAsync(
        CreateTeacherSubjectAssignmentRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateTeacherSubjectAssignmentAsync(
        Guid id,
        UpdateTeacherSubjectAssignmentRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateTeacherSubjectAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateTeacherSubjectAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken);
}
