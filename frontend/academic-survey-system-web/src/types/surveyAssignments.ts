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

export interface SurveyAssignmentFilters {
  includeInactive: boolean;
  surveyId: string;
  careerId: string;
  subjectId: string;
  academicCycleId: string;
  teacherId: string;
}

export interface CreateSurveyAssignmentRequest {
  surveyId: string;
  careerId: string;
  subjectId: string;
  academicCycleId: string;
  teacherSubjectAssignmentId: string;
}

export interface CreateSurveyAssignmentBatchRequest {
  surveyId: string;
  careerId: string;
  subjectId: string;
  academicCycleId: string;
  teacherSubjectAssignmentIds: string[];
}
