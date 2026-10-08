import { BrowserRouter, Navigate, Route, Routes, useParams } from 'react-router-dom';
import { ToastProvider } from './components/ui/ToastProvider';
import { AuthProvider, useAuth } from './auth/AuthProvider';
import { AuthLoadingScreen, ProtectedRoute } from './auth/ProtectedRoute';
import { AcademicContextProvider } from './features/academic-context/AcademicContextProvider';
import {
  AcademicUnitsPage,
  AcademicUnitPage,
  CareerWorkspacePage,
  CareerWorkspaceRedirect
} from './features/academic-context/AcademicWorkspacePages';
import {
  CareerOverviewPage,
  ContextSubjectsPage,
  ContextTeachersPage
} from './features/academic-context/CareerContextPages';
import {
  AcademicCyclesCatalogPage,
  CareersCatalogPage,
  SubjectsCatalogPage,
  TeacherAssignmentsCatalogPage,
  TeachersCatalogPage
} from './features/academic-catalog/AcademicCatalogPages';
import { AppShell } from './features/app-shell/AppShell';
import { AuditPage } from './features/audit/AuditPage';
import { LoginPage } from './features/auth/LoginPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { PublicSurveyPage } from './features/public-survey/PublicSurveyPage';
import { ResultsPage } from './features/results/ResultsPage';
import { SurveyHistoryPage } from './features/results/SurveyHistoryPage';
import { SurveyAssignmentReportPage } from './features/results/SurveyAssignmentReportPage';
import { SurveyAssignmentResultsPage } from './features/results/SurveyAssignmentResultsPage';
import { SurveyAssignmentCreatePage } from './features/survey-assignments/SurveyAssignmentCreatePage';
import { SurveyAssignmentsPage } from './features/survey-assignments/SurveyAssignmentsPage';
import { SurveySessionsPage } from './features/survey-sessions/SurveySessionsPage';
import { SurveyCreatePage } from './features/surveys/SurveyCreatePage';
import { SurveyEditorPage } from './features/surveys/SurveyEditorPage';
import { SurveyPreviewPage } from './features/surveys/SurveyPreviewPage';
import { SurveysPage } from './features/surveys/SurveysPage';
import { UserCreatePage, UserDetailPage, UsersPage } from './features/users/UsersPages';

export function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
      <AuthProvider>
        <AcademicContextProvider>
          <Routes>
          <Route element={<RootRoute />} path="/" />
          <Route element={<LoginPage />} path="/login" />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <DashboardPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app"
          />
          <Route
            element={
              <ProtectedRoute requiredPermission="audit.read">
                <AppShell>
                  <AuditPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/audit"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <UsersPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/users"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <UserCreatePage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/users/new"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <UserDetailPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/users/:userId"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveySessionsPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/sessions"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/context"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/context/subjects"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/context/teachers"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/context/teacher-assignments"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <CareersCatalogPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic/careers"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <AcademicUnitsPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic/units"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <AcademicUnitPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic/units/:unitId"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <CareerWorkspacePage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic/units/:unitId/careers/:careerId"
          >
            <Route index element={<CareerWorkspaceRedirect />} />
            <Route element={<CareerOverviewPage />} path="overview" />
            <Route element={<ContextSubjectsPage />} path="subjects" />
            <Route element={<ContextTeachersPage />} path="teachers" />
            <Route element={<SurveyAssignmentsPage contextual />} path="surveys" />
            <Route element={<SurveyAssignmentCreatePage contextual />} path="surveys/new" />
            <Route element={<SurveySessionsPage contextual />} path="sessions" />
            <Route element={<ResultsPage contextual />} path="results" />
            <Route element={<SurveyHistoryPage />} path="history" />
          </Route>
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SubjectsCatalogPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic/subjects"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <AcademicCyclesCatalogPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic/cycles"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <TeachersCatalogPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic/teachers"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <TeacherAssignmentsCatalogPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/academic/teacher-assignments"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveysPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/surveys"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveyCreatePage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/surveys/new"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveyAssignmentsPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/survey-assignments"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/context/surveys"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveyAssignmentCreatePage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/survey-assignments/new"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/context/surveys/new"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <ResultsPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/results"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveyHistoryPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/results/history"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/context/results"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <Navigate replace to="/app/academic/units" />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/context/sessions"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveyAssignmentResultsPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/results/assignments/:surveyAssignmentId"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveyAssignmentReportPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/results/assignments/:surveyAssignmentId/report"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveyEditorPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/surveys/:surveyId/edit"
          />
          <Route
            element={
              <ProtectedRoute>
                <AppShell>
                  <SurveyPreviewPage />
                </AppShell>
              </ProtectedRoute>
            }
            path="/app/surveys/:surveyId/preview"
          />
          <Route element={<PublicSurveyRoute />} path="/survey/:accessCode" />
          <Route element={<Navigate replace to="/" />} path="*" />
          </Routes>
        </AcademicContextProvider>
      </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  );
}

function RootRoute() {
  const auth = useAuth();

  if (auth.isLoading) {
    return <AuthLoadingScreen />;
  }

  return <Navigate replace to={auth.isAuthenticated ? '/app' : '/login'} />;
}

function PublicSurveyRoute() {
  const { accessCode } = useParams();

  if (!accessCode) {
    return (
      <main className="survey-shell">
        <section className="status-panel">
          <p className="eyebrow">Sistema Web de Gestión de Encuestas Académicas</p>
          <h1>Encuesta académica</h1>
          <p>Ingresá desde el enlace o código QR provisto por la institución.</p>
        </section>
      </main>
    );
  }

  return <PublicSurveyPage accessCode={accessCode} />;
}
