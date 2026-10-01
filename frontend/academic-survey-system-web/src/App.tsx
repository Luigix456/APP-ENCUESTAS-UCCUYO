import { BrowserRouter, Navigate, Route, Routes, useParams } from 'react-router-dom';
import { AuthProvider, useAuth } from './auth/AuthProvider';
import { AuthLoadingScreen, ProtectedRoute } from './auth/ProtectedRoute';
import { AcademicCatalogHomePage } from './features/academic-catalog/AcademicCatalogHomePage';
import {
  AcademicCyclesCatalogPage,
  CareersCatalogPage,
  SubjectsCatalogPage,
  TeacherAssignmentsCatalogPage,
  TeachersCatalogPage
} from './features/academic-catalog/AcademicCatalogPages';
import { AppShell } from './features/app-shell/AppShell';
import { LoginPage } from './features/auth/LoginPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { PublicSurveyPage } from './features/public-survey/PublicSurveyPage';
import { ResultsPage } from './features/results/ResultsPage';
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
      <AuthProvider>
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
                  <AcademicCatalogHomePage />
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
      </AuthProvider>
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
