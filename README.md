# Sistema Web de Gestión de Encuestas Académicas

Aplicación web para la gestión de encuestas académicas. El backend usa Clean Architecture y cuenta con infraestructura inicial de persistencia configurada con Entity Framework Core y PostgreSQL.

## Tecnologías iniciales

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
- PostgreSQL local para probar la conexión real
- Herramienta `dotnet-ef` para ejecutar comandos de migración

## PostgreSQL

Base local esperada:

```text
academic_survey_db
```

La cadena de conexión se lee desde `ConnectionStrings:DefaultConnection` y puede sobrescribirse con la variable de entorno `ConnectionStrings__DefaultConnection`.

Las credenciales reales deben proporcionarse mediante variable de entorno. No se deben almacenar secretos reales ni credenciales de producción en el repositorio.

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=TU_PASSWORD"
```

Bash:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=TU_PASSWORD"
```

`appsettings.Development.json` contiene una cadena local de ejemplo para desarrollo.

## Ejecutar Backend

```bash
cd backend
dotnet restore AcademicSurveySystem.sln
dotnet run --project src/AcademicSurveySystem.Api/AcademicSurveySystem.Api.csproj
```

## Health Check

Endpoint:

```text
GET /api/health
```

PowerShell:

```powershell
Invoke-RestMethod http://localhost:5047/api/health
```

Bash:

```bash
curl http://localhost:5047/api/health
```

El endpoint valida que la API esté activa y que `ApplicationDbContext` pueda conectarse a PostgreSQL.

Resultado esperado cuando PostgreSQL está disponible y la cadena de conexión es válida:

```text
HTTP 200
```

```json
{
  "status": "Healthy",
  "service": "AcademicSurveySystem.Api",
  "checks": [
    {
      "name": "ApplicationDbContext",
      "status": "Healthy"
    }
  ]
}
```

Si PostgreSQL no está disponible o la cadena de conexión no es válida, el health check debe reflejar el estado real y devolver `HTTP 503`.

## Entity Framework Core

La migración `InitialInfrastructure` es una migración vacía porque todavía no existen entidades de negocio. Al aplicarla, Entity Framework Core puede crear la tabla técnica `__EFMigrationsHistory` para registrar migraciones aplicadas.

Crear una migración:

```bash
dotnet ef migrations add NombreMigracion \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api \
  --output-dir Persistence/Migrations
```

Listar migraciones:

```bash
dotnet ef migrations list \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api
```

Aplicar migraciones:

```bash
dotnet ef database update \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api
```

Consultar información del DbContext:

```bash
dotnet ef dbcontext info \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api
```

## Ejecutar Frontend

```bash
cd frontend/academic-survey-system-web
npm install
npm run dev
```

## Estado Actual

Infraestructura inicial de persistencia configurada. Todavía no existen entidades de negocio, autenticación, usuarios, roles, permisos, repositorios, casos de uso ni funcionalidades del sistema.
