import type { SurveyQuestionResultsDto } from './results';

export interface ReportInstitutionDto {
  institutionName: string;
  systemName: string;
  facultyName: string | null;
}

export interface SurveyReportDto {
  institution: ReportInstitutionDto;
  generatedAtUtc: string;
  surveyAssignmentId: string;
  surveyId: string;
  surveyTitle: string;
  surveyVersionNumber: number;
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
  totalResponses: number;
  totalSessions: number;
  firstSubmittedAtUtc: string | null;
  lastSubmittedAtUtc: string | null;
  questions: SurveyQuestionResultsDto[];
  expectedRespondentCount: number | null;
  remainingCount: number | null;
  participationPercentage: number | null;
  detailedResultsAvailable: boolean;
  minimumResponsesRequired: number;
  responsesNeededToUnlock: number;
}
