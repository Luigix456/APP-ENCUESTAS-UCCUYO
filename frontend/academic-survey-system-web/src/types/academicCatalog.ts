export type CareerType = 'Undergraduate' | 'Postgraduate' | 'Course' | 'Other';

export type SubjectPeriod = 'Annual' | 'FirstSemester' | 'SecondSemester';

export type AcademicCyclePeriod = 'Annual' | 'FirstSemester' | 'SecondSemester';

export interface AcademicUnitDto {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CareerDto {
  id: string;
  academicUnitId: string;
  academicUnitCode: string;
  academicUnitName: string;
  code: string;
  name: string;
  type: CareerType;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SubjectDto {
  id: string;
  careerId: string;
  careerName: string;
  code: string;
  name: string;
  year: number;
  period: SubjectPeriod;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AcademicCycleDto {
  id: string;
  year: number;
  period: AcademicCyclePeriod;
  startDate: string;
  endDate: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface TeacherDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface TeacherSubjectAssignmentDto {
  id: string;
  teacherId: string;
  teacherFullName: string;
  subjectId: string;
  subjectName: string;
  careerId: string;
  careerName: string;
  academicCycleId: string;
  academicCycleYear: number;
  academicCyclePeriod: AcademicCyclePeriod;
  teachingRole: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SubjectEnrollmentDto {
  id: string;
  subjectId: string;
  subjectName: string;
  careerId: string;
  careerName: string;
  academicCycleId: string;
  academicCycleYear: number;
  academicCyclePeriod: AcademicCyclePeriod;
  enrolledStudentCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AcademicAttentionDto {
  items: AcademicAttentionItemDto[];
}

export interface AcademicAttentionItemDto {
  code: string;
  severity: 'info' | 'warning';
  title: string;
  description: string;
  entityType: string;
  entityId: string | null;
  actionCode: string;
  count: number;
}

export interface SetSubjectEnrollmentRequest {
  enrolledStudentCount: number;
}

export type SubjectEnrollmentImportRowStatus = 'Create' | 'Update' | 'Unchanged' | 'Error';

export interface SubjectEnrollmentImportPreviewRowDto {
  rowNumber: number;
  subjectCode: string;
  providedSubjectName: string | null;
  subjectId: string | null;
  subjectName: string | null;
  currentEnrolledStudentCount: number | null;
  newEnrolledStudentCount: number | null;
  status: SubjectEnrollmentImportRowStatus;
  errorCode: string | null;
  errorMessage: string | null;
}

export interface SubjectEnrollmentImportPreviewDto {
  fileName: string;
  totalRows: number;
  validRows: number;
  createRows: number;
  updateRows: number;
  unchangedRows: number;
  errorRows: number;
  rows: SubjectEnrollmentImportPreviewRowDto[];
}

export interface SubjectEnrollmentImportResultDto {
  createdCount: number;
  updatedCount: number;
  unchangedCount: number;
  totalProcessed: number;
}

export interface TeacherSubjectAssignmentFilters {
  includeInactive: boolean;
  careerId?: string;
  teacherId?: string;
  subjectId?: string;
  academicCycleId?: string;
}

export interface CreateAcademicUnitRequest {
  code: string;
  name: string;
}

export interface UpdateAcademicUnitRequest {
  name: string;
}

export interface CreateCareerRequest {
  academicUnitId: string;
  code: string;
  name: string;
  type: CareerType;
}

export interface UpdateCareerRequest {
  name: string;
  type: CareerType;
}

export interface CreateSubjectRequest {
  careerId: string;
  code: string;
  name: string;
  year: number;
  period: SubjectPeriod;
}

export interface UpdateSubjectRequest {
  name: string;
  year: number;
  period: SubjectPeriod;
}

export interface CreateAcademicCycleRequest {
  year: number;
  period: AcademicCyclePeriod;
  startDate: string;
  endDate: string;
}

export interface UpdateAcademicCycleRequest {
  period: AcademicCyclePeriod;
  startDate: string;
  endDate: string;
}

export interface CreateTeacherRequest {
  firstName: string;
  lastName: string;
  email: string | null;
}

export type UpdateTeacherRequest = CreateTeacherRequest;

export interface CreateTeacherSubjectAssignmentRequest {
  teacherId: string;
  subjectId: string;
  academicCycleId: string;
  teachingRole: string;
}

export interface UpdateTeacherSubjectAssignmentRequest {
  teachingRole: string;
}

export const CAREER_TYPES: CareerType[] = ['Undergraduate', 'Postgraduate', 'Course', 'Other'];

export const SUBJECT_PERIODS: SubjectPeriod[] = ['Annual', 'FirstSemester', 'SecondSemester'];

export const ACADEMIC_CYCLE_PERIODS: AcademicCyclePeriod[] = [
  'Annual',
  'FirstSemester',
  'SecondSemester'
];

export const CAREER_TYPE_LABELS: Record<CareerType, string> = {
  Undergraduate: 'Grado / pregrado',
  Postgraduate: 'Posgrado',
  Course: 'Curso',
  Other: 'Otro'
};

export const ACADEMIC_PERIOD_LABELS: Record<SubjectPeriod | AcademicCyclePeriod, string> = {
  Annual: 'Anual',
  FirstSemester: 'Primer semestre',
  SecondSemester: 'Segundo semestre'
};
