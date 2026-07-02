# Sistema Web de Gestion de Encuestas Academicas

Aplicacion web para la gestion de encuestas academicas. El backend usa Clean Architecture y ya cuenta con la infraestructura inicial de persistencia configurada para Entity Framework Core y PostgreSQL.

## Tecnologias iniciales

- .NET 8
- ASP.NET Core Web API
- C#
- Clean Architecture
- Entity Framework Core 8
- PostgreSQL
- Npgsql.EntityFrameworkCore.PostgreSQL
- React
- Vite
- TypeScript
- xUnit

## Estructura del repositorio

```text
/
|-- backend/
|   |-- AcademicSurveySystem.sln
|   |-- src/
|   |   |-- AcademicSurveySystem.Domain/
|   |   |-- AcademicSurveySystem.Application/
|   |   |-- AcademicSurveySystem.Infrastructure/
|   |   `-- AcademicSurveySystem.Api/
|   `-- tests/
|       |-- AcademicSurveySystem.UnitTests/
|       `-- AcademicSurveySystem.IntegrationTests/
|-- frontend/
|   `-- academic-survey-system-web/
|-- .gitignore
`-- README.md
```

## Requisitos previos

- .NET SDK 8 o superior con soporte para `net8.0`
- Node.js y npm
- PostgreSQL local, si se desea probar la conexion real
- Herramienta `dotnet-ef` para ejecutar comandos de migracion

## PostgreSQL

Base local sugerida:

```text
academic_survey_db
```

La cadena de conexion se lee desde `ConnectionStrings:DefaultConnection` y puede sobrescribirse con la variable de entorno `ConnectionStrings__DefaultConnection`.

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=TU_PASSWORD"
```

Bash:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=TU_PASSWORD"
```

`appsettings.Development.json` contiene una cadena local de ejemplo. No se deben almacenar secretos reales ni credenciales de produccion en el repositorio.

## Ejecutar backend

```bash
cd backend
dotnet restore
dotnet run --project src/AcademicSurveySystem.Api/AcademicSurveySystem.Api.csproj
```

Health check:

```text
GET /api/health
```

El endpoint valida que la API este activa y que `ApplicationDbContext` pueda conectarse a PostgreSQL.

## Entity Framework Core

Crear una migracion:

```bash
dotnet ef migrations add NombreMigracion \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api \
  --output-dir Persistence/Migrations
```

Aplicar migraciones:

```bash
dotnet ef database update \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api
```

Consultar informacion del DbContext:

```bash
dotnet ef dbcontext info \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api
```

## Ejecutar frontend

```bash
cd frontend/academic-survey-system-web
npm install
npm run dev
```

## Estado actual

Infraestructura inicial de persistencia configurada. Todavia no existen entidades de negocio, autenticacion, usuarios, roles, permisos, repositorios, casos de uso ni funcionalidades del sistema.
