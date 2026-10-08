import { apiRequest, apiRequestBlob, type ApiBinaryResponse } from './apiClient';
import type {
  AcademicCycleDto,
  AcademicAttentionDto,
  AcademicUnitDto,
  CareerDto,
  CreateAcademicCycleRequest,
  CreateAcademicUnitRequest,
  CreateCareerRequest,
  CreateSubjectRequest,
  CreateTeacherRequest,
  CreateTeacherSubjectAssignmentRequest,
  SetSubjectEnrollmentRequest,
  SubjectDto,
  SubjectEnrollmentDto,
  SubjectEnrollmentImportPreviewDto,
  SubjectEnrollmentImportResultDto,
  TeacherDto,
  TeacherSubjectAssignmentDto,
  TeacherSubjectAssignmentFilters,
  UpdateAcademicCycleRequest,
  UpdateAcademicUnitRequest,
  UpdateCareerRequest,
  UpdateSubjectRequest,
  UpdateTeacherRequest,
  UpdateTeacherSubjectAssignmentRequest
} from '../types/academicCatalog';

type UnauthorizedHandler = () => void;

interface CatalogRequestOptions {
  accessToken: string;
  onUnauthorized: UnauthorizedHandler;
  signal?: AbortSignal;
}

interface ListCatalogRequestOptions extends CatalogRequestOptions {
  includeInactive?: boolean;
}

interface ListCareersRequestOptions extends ListCatalogRequestOptions {
  academicUnitId?: string;
}

interface ListSubjectEnrollmentsRequestOptions extends CatalogRequestOptions {
  subjectId?: string;
  academicCycleId?: string;
  careerId?: string;
}

export function getAcademicUnits({
  accessToken,
  onUnauthorized,
  includeInactive = false,
  signal
}: ListCatalogRequestOptions): Promise<AcademicUnitDto[]> {
  return apiRequest<AcademicUnitDto[]>(
    `/api/academic/academic-units?includeInactive=${includeInactive}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getAcademicUnit(
  id: string,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<AcademicUnitDto> {
  return apiRequest<AcademicUnitDto>(`/api/academic/academic-units/${encodeURIComponent(id)}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function createAcademicUnit(
  request: CreateAcademicUnitRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<AcademicUnitDto> {
  return apiRequest<AcademicUnitDto>('/api/academic/academic-units', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function updateAcademicUnit(
  id: string,
  request: UpdateAcademicUnitRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/academic/academic-units/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function activateAcademicUnit(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/academic-units/${encodeURIComponent(id)}/activate`, options);
}

export function deactivateAcademicUnit(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/academic-units/${encodeURIComponent(id)}/deactivate`, options);
}

export function getCareers({
  accessToken,
  onUnauthorized,
  academicUnitId,
  includeInactive = false,
  signal
}: ListCareersRequestOptions): Promise<CareerDto[]> {
  const searchParams = new URLSearchParams({
    includeInactive: String(includeInactive)
  });

  if (academicUnitId) {
    searchParams.set('academicUnitId', academicUnitId);
  }

  return apiRequest<CareerDto[]>(`/api/academic/careers?${searchParams.toString()}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function getCareer(
  id: string,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<CareerDto> {
  return apiRequest<CareerDto>(`/api/academic/careers/${encodeURIComponent(id)}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function createCareer(
  request: CreateCareerRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<CareerDto> {
  return apiRequest<CareerDto>('/api/academic/careers', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function updateCareer(
  id: string,
  request: UpdateCareerRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/academic/careers/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function activateCareer(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/careers/${encodeURIComponent(id)}/activate`, options);
}

export function deactivateCareer(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/careers/${encodeURIComponent(id)}/deactivate`, options);
}

export function getCareerTeachers(
  careerId: string,
  {
    accessToken,
    academicCycleId,
    includeInactive = false,
    onUnauthorized,
    signal
  }: ListCatalogRequestOptions & { academicCycleId?: string }
): Promise<TeacherDto[]> {
  const searchParams = new URLSearchParams({
    includeInactive: String(includeInactive)
  });

  if (academicCycleId) {
    searchParams.set('academicCycleId', academicCycleId);
  }

  return apiRequest<TeacherDto[]>(
    `/api/academic/careers/${encodeURIComponent(careerId)}/teachers?${searchParams.toString()}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getCareerAttention(
  careerId: string,
  academicCycleId: string,
  { accessToken, onUnauthorized, signal }: CatalogRequestOptions
): Promise<AcademicAttentionDto> {
  const searchParams = new URLSearchParams({
    academicCycleId
  });

  return apiRequest<AcademicAttentionDto>(
    `/api/academic/careers/${encodeURIComponent(careerId)}/attention?${searchParams.toString()}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getSubjects(
  careerId: string,
  { accessToken, onUnauthorized, signal, includeInactive = false }: ListCatalogRequestOptions
): Promise<SubjectDto[]> {
  const searchParams = new URLSearchParams({
    includeInactive: String(includeInactive)
  });

  if (careerId) {
    searchParams.set('careerId', careerId);
  }

  return apiRequest<SubjectDto[]>(`/api/academic/subjects?${searchParams.toString()}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function getSubject(
  id: string,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<SubjectDto> {
  return apiRequest<SubjectDto>(`/api/academic/subjects/${encodeURIComponent(id)}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function createSubject(
  request: CreateSubjectRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<SubjectDto> {
  return apiRequest<SubjectDto>('/api/academic/subjects', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function updateSubject(
  id: string,
  request: UpdateSubjectRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/academic/subjects/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function activateSubject(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/subjects/${encodeURIComponent(id)}/activate`, options);
}

export function deactivateSubject(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/subjects/${encodeURIComponent(id)}/deactivate`, options);
}

export function getSubjectEnrollments({
  accessToken,
  onUnauthorized,
  signal,
  subjectId,
  academicCycleId,
  careerId
}: ListSubjectEnrollmentsRequestOptions): Promise<SubjectEnrollmentDto[]> {
  const searchParams = new URLSearchParams();

  appendOptionalFilter(searchParams, 'subjectId', subjectId);
  appendOptionalFilter(searchParams, 'academicCycleId', academicCycleId);
  appendOptionalFilter(searchParams, 'careerId', careerId);

  const query = searchParams.toString();

  return apiRequest<SubjectEnrollmentDto[]>(
    `/api/academic/subject-enrollments${query ? `?${query}` : ''}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getSubjectEnrollment(
  subjectId: string,
  academicCycleId: string,
  { accessToken, onUnauthorized, signal }: CatalogRequestOptions
): Promise<SubjectEnrollmentDto> {
  return apiRequest<SubjectEnrollmentDto>(
    `/api/academic/subjects/${encodeURIComponent(subjectId)}/enrollment?academicCycleId=${encodeURIComponent(academicCycleId)}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function setSubjectEnrollment(
  subjectId: string,
  academicCycleId: string,
  enrolledStudentCount: number,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<SubjectEnrollmentDto> {
  const request: SetSubjectEnrollmentRequest = { enrolledStudentCount };

  return apiRequest<SubjectEnrollmentDto>(
    `/api/academic/subjects/${encodeURIComponent(subjectId)}/enrollment/${encodeURIComponent(academicCycleId)}`,
    {
      method: 'PUT',
      body: request,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function downloadSubjectEnrollmentImportTemplate(
  careerId: string,
  academicCycleId: string,
  format: 'xlsx' | 'csv',
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<ApiBinaryResponse> {
  const searchParams = new URLSearchParams({
    careerId,
    academicCycleId,
    format
  });

  return apiRequestBlob(`/api/academic/subject-enrollments/import-template?${searchParams.toString()}`, {
    token: accessToken,
    onUnauthorized,
    accept: format === 'xlsx'
      ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      : 'text/csv'
  });
}

export function previewSubjectEnrollmentImport(
  careerId: string,
  academicCycleId: string,
  file: File,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<SubjectEnrollmentImportPreviewDto> {
  const formData = new FormData();
  formData.append('file', file);
  const searchParams = new URLSearchParams({ careerId, academicCycleId });

  return apiRequest<SubjectEnrollmentImportPreviewDto>(
    `/api/academic/subject-enrollments/import/preview?${searchParams.toString()}`,
    {
      method: 'POST',
      body: formData,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function importSubjectEnrollments(
  careerId: string,
  academicCycleId: string,
  file: File,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<SubjectEnrollmentImportResultDto> {
  const formData = new FormData();
  formData.append('file', file);
  const searchParams = new URLSearchParams({ careerId, academicCycleId });

  return apiRequest<SubjectEnrollmentImportResultDto>(
    `/api/academic/subject-enrollments/import?${searchParams.toString()}`,
    {
      method: 'POST',
      body: formData,
      token: accessToken,
      onUnauthorized
    }
  );
}

export function getAcademicCycles({
  accessToken,
  onUnauthorized,
  includeInactive = false,
  signal
}: ListCatalogRequestOptions): Promise<AcademicCycleDto[]> {
  return apiRequest<AcademicCycleDto[]>(`/api/academic/academic-cycles?includeInactive=${includeInactive}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function getAcademicCycle(
  id: string,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<AcademicCycleDto> {
  return apiRequest<AcademicCycleDto>(`/api/academic/academic-cycles/${encodeURIComponent(id)}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function createAcademicCycle(
  request: CreateAcademicCycleRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<AcademicCycleDto> {
  return apiRequest<AcademicCycleDto>('/api/academic/academic-cycles', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function updateAcademicCycle(
  id: string,
  request: UpdateAcademicCycleRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/academic/academic-cycles/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function activateAcademicCycle(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/academic-cycles/${encodeURIComponent(id)}/activate`, options);
}

export function deactivateAcademicCycle(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/academic-cycles/${encodeURIComponent(id)}/deactivate`, options);
}

export function getTeachers({
  accessToken,
  onUnauthorized,
  includeInactive = false,
  signal
}: ListCatalogRequestOptions): Promise<TeacherDto[]> {
  return apiRequest<TeacherDto[]>(`/api/academic/teachers?includeInactive=${includeInactive}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

export function getTeacher(
  id: string,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<TeacherDto> {
  return apiRequest<TeacherDto>(`/api/academic/teachers/${encodeURIComponent(id)}`, {
    token: accessToken,
    onUnauthorized
  });
}

export function createTeacher(
  request: CreateTeacherRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<TeacherDto> {
  return apiRequest<TeacherDto>('/api/academic/teachers', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function updateTeacher(
  id: string,
  request: UpdateTeacherRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/academic/teachers/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function activateTeacher(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/teachers/${encodeURIComponent(id)}/activate`, options);
}

export function deactivateTeacher(id: string, options: CatalogRequestOptions): Promise<null> {
  return patchCatalogCommand(`/api/academic/teachers/${encodeURIComponent(id)}/deactivate`, options);
}

export function getTeacherSubjectAssignments(
  filters: TeacherSubjectAssignmentFilters,
  { accessToken, onUnauthorized, signal }: CatalogRequestOptions
): Promise<TeacherSubjectAssignmentDto[]> {
  const searchParams = new URLSearchParams({
    includeInactive: String(filters.includeInactive)
  });

  if (filters.teacherId) {
    searchParams.set('teacherId', filters.teacherId);
  }

  if (filters.careerId) {
    searchParams.set('careerId', filters.careerId);
  }

  if (filters.subjectId) {
    searchParams.set('subjectId', filters.subjectId);
  }

  if (filters.academicCycleId) {
    searchParams.set('academicCycleId', filters.academicCycleId);
  }

  return apiRequest<TeacherSubjectAssignmentDto[]>(
    `/api/academic/teacher-subject-assignments?${searchParams.toString()}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}

export function getTeacherSubjectAssignment(
  id: string,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<TeacherSubjectAssignmentDto> {
  return apiRequest<TeacherSubjectAssignmentDto>(
    `/api/academic/teacher-subject-assignments/${encodeURIComponent(id)}`,
    {
      token: accessToken,
      onUnauthorized
    }
  );
}

export function createTeacherSubjectAssignment(
  request: CreateTeacherSubjectAssignmentRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<TeacherSubjectAssignmentDto> {
  return apiRequest<TeacherSubjectAssignmentDto>('/api/academic/teacher-subject-assignments', {
    method: 'POST',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function updateTeacherSubjectAssignment(
  id: string,
  request: UpdateTeacherSubjectAssignmentRequest,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<null> {
  return apiRequest<null>(`/api/academic/teacher-subject-assignments/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: request,
    token: accessToken,
    onUnauthorized
  });
}

export function activateTeacherSubjectAssignment(
  id: string,
  options: CatalogRequestOptions
): Promise<null> {
  return patchCatalogCommand(
    `/api/academic/teacher-subject-assignments/${encodeURIComponent(id)}/activate`,
    options
  );
}

export function deactivateTeacherSubjectAssignment(
  id: string,
  options: CatalogRequestOptions
): Promise<null> {
  return patchCatalogCommand(
    `/api/academic/teacher-subject-assignments/${encodeURIComponent(id)}/deactivate`,
    options
  );
}

function patchCatalogCommand(
  path: string,
  { accessToken, onUnauthorized }: CatalogRequestOptions
): Promise<null> {
  return apiRequest<null>(path, {
    method: 'PATCH',
    token: accessToken,
    onUnauthorized
  });
}

function appendOptionalFilter(searchParams: URLSearchParams, key: string, value?: string) {
  if (value) {
    searchParams.set(key, value);
  }
}
