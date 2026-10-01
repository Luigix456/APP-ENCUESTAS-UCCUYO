export type SurveySessionStatus = 'Created' | 'Open' | 'Closed' | 'Cancelled' | 'Expired';

export interface SurveyAssignmentDto {
  id: string;
  surveyId: string;
  surveyTitle: string;
  surveyStatus: string;
  careerId: string;
  careerName: string;
  subjectId: string;
  subjectName: string;
  academicCycleId: string;
  academicCycleYear: number;
  academicCyclePeriod: string;
  teacherSubjectAssignmentId: string;
  teacherId: string;
  teacherFullName: string;
  teachingRole: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SurveySessionDto {
  id: string;
  surveyAssignmentId: string;
  accessCode: string;
  publicPath: string;
  publicUrl: string | null;
  title: string | null;
  location: string | null;
  status: SurveySessionStatus;
  expiresAtUtc: string;
  openedAtUtc: string | null;
  closedAtUtc: string | null;
  isActive: boolean;
  surveyId: string;
  surveyTitle: string;
  careerId: string;
  careerName: string;
  subjectId: string;
  subjectName: string;
  academicCycleId: string;
  academicCycleYear: number;
  academicCyclePeriod: string;
  teacherId: string;
  teacherFullName: string;
  teachingRole: string;
  createdByUserId: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateSurveySessionRequest {
  surveyAssignmentId: string;
  title: string | null;
  location: string | null;
  expiresAtUtc: string;
}
