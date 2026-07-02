# Sistema Web de Gestión de Encuestas Académicas

Aplicación web para la gestión de encuestas académicas. El repositorio inicia con una arquitectura limpia para el backend y una aplicación React mínima para el frontend.

## Tecnologías iniciales

- .NET 8
- ASP.NET Core Web API
- C#
- Clean Architecture
- React
- Vite
- TypeScript
- xUnit

## Estructura del repositorio

```text
/
├── backend/
│   ├── AcademicSurveySystem.sln
│   ├── src/
│   │   ├── AcademicSurveySystem.Domain/
│   │   ├── AcademicSurveySystem.Application/
│   │   ├── AcademicSurveySystem.Infrastructure/
│   │   └── AcademicSurveySystem.Api/
│   └── tests/
│       ├── AcademicSurveySystem.UnitTests/
│       └── AcademicSurveySystem.IntegrationTests/
├── frontend/
│   └── academic-survey-system-web/
├── .gitignore
└── README.md
```

## Requisitos previos

- .NET SDK 8 o superior con soporte para `net8.0`
- Node.js y npm

## Ejecutar backend

```bash
cd backend
dotnet restore
dotnet run --project src/AcademicSurveySystem.Api/AcademicSurveySystem.Api.csproj
```

El endpoint inicial de salud está disponible en:

```text
GET /api/health
```

## Ejecutar frontend

```bash
cd frontend/academic-survey-system-web
npm install
npm run dev
```

## Estado actual

Estructura inicial creada. No se implementaron entidades, autenticación, base de datos, repositorios, casos de uso ni funcionalidades de negocio.
