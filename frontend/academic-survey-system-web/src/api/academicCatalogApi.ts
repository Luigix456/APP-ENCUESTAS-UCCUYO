import { apiRequest } from './apiClient';
import type {
  AcademicCycleDto,
  CareerDto,
  CreateAcademicCycleRequest,
  CreateCareerRequest,
  CreateSubjectRequest,
  CreateTeacherRequest,
  CreateTeacherSubjectAssignmentRequest,
  SubjectDto,
  TeacherDto,
  TeacherSubjectAssignmentDto,
  TeacherSubjectAssignmentFilters,
  UpdateAcademicCycleRequest,
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

export function getCareers({
  accessToken,
  onUnauthorized,
  includeInactive = false,
  signal
}: ListCatalogRequestOptions): Promise<CareerDto[]> {
  return apiRequest<CareerDto[]>(`/api/academic/careers?includeInactive=${includeInactive}`, {
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
