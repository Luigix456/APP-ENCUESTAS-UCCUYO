export interface CareerParticipationDashboardDto {
  careerId: string;
  careerName: string;
  academicCycleId: string;
  academicCycleLabel: string;
  totalSubjects: number;
  subjectsWithResponses: number;
  studentSurveyAssignmentsCount: number;
  openSessionsCount: number;
  averageParticipationPercentage: number | null;
  lowParticipationAssignmentsCount: number;
  lowParticipationThresholdPercentage: number;
  items: CareerParticipationDashboardItemDto[];
}

export interface CareerParticipationDashboardItemDto {
  surveyAssignmentId: string;
  subjectId: string;
  subjectName: string;
  teacherId: string;
  teacherName: string;
  surveyId: string;
  surveyTitle: string;
  surveyVersionNumber: number;
  expectedRespondentCount: number | null;
  responseCount: number;
  remainingCount: number | null;
  participationPercentage: number | null;
  hasResponses: boolean;
  detailedResultsAvailable: boolean;
  isLowParticipation: boolean;
}
