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
  expectedRespondentCount: number | null;
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
  sessionResponseCount: number;
  assignmentResponseCount: number;
  expectedRespondentCount: number | null;
  remainingCount: number | null;
  participationPercentage: number | null;
}

export interface SurveyResponseProgressDto {
  surveyAssignmentId: string;
  expectedRespondentCount: number | null;
  responseCount: number;
  remainingCount: number | null;
  participationPercentage: number | null;
}

export interface CreateSurveySessionRequest {
  surveyAssignmentId: string;
  title: string | null;
  location: string | null;
  expiresAtUtc: string;
}
