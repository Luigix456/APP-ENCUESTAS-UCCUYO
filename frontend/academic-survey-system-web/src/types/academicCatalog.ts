export type CareerType = 'Undergraduate' | 'Postgraduate' | 'Course' | 'Other';

export type SubjectPeriod = 'Annual' | 'FirstSemester' | 'SecondSemester';

export type AcademicCyclePeriod = 'Annual' | 'FirstSemester' | 'SecondSemester';

export interface CareerDto {
  id: string;
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

export interface TeacherSubjectAssignmentFilters {
  includeInactive: boolean;
  teacherId?: string;
  subjectId?: string;
  academicCycleId?: string;
}

export interface CreateCareerRequest {
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
