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
- BCrypt.Net-Next 4.2.0
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.30
- System.IdentityModel.Tokens.Jwt 8.22.0
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

## Seguridad de Contraseñas

Las contraseñas no se almacenan en texto plano. La infraestructura usa `BCrypt.Net-Next` 4.2.0 y guarda únicamente hashes BCrypt.

La configuración no sensible del factor de trabajo se encuentra en `Security:PasswordHashing:WorkFactor`. El valor predeterminado es `12` y el rango permitido es de `10` a `16`.

Política mínima para la contraseña del administrador inicial:

- Mínimo 12 caracteres
- Máximo 64 caracteres
- Máximo 72 bytes en UTF-8
- Al menos una letra mayúscula
- Al menos una letra minúscula
- Al menos un número
- Al menos un carácter no alfanumérico

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

La migración `AddIdentityCore` agrega el núcleo persistente de identidad y autorización. Esa migración no inserta usuarios ni contraseñas y no modifica la configuración de autenticación.

La migración `SeedIdentityCatalog` agrega un catálogo inicial determinista de cuatro roles institucionales y catorce permisos. Los estudiantes no poseen rol porque responderán encuestas públicas y anónimas mediante sesiones QR.

Tablas creadas por el núcleo de identidad:

- `users`: usuarios internos del sistema. Almacena datos básicos, estado y `password_hash`; no almacena contraseñas en texto plano.
- `roles`: roles asignables a usuarios.
- `permissions`: permisos del sistema agrupados por módulo.
- `user_roles`: relación muchos a muchos entre usuarios y roles.
- `role_permissions`: relación muchos a muchos entre roles y permisos.

Roles institucionales iniciales:

| Rol | Finalidad |
| --- | --- |
| Administrador | Administración completa |
| Encuestadora | Gestión de sesiones de encuesta |
| Decana | Consulta institucional |
| Director de carrera | Consulta limitada a su carrera |

Crear una migración:

```bash
dotnet ef migrations add NombreMigracion \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api \
  --output-dir Persistence/Migrations
```

Crear la migración del núcleo de identidad:

```bash
dotnet ef migrations add AddIdentityCore \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api \
  --output-dir Persistence/Migrations
```

Crear la migración del catálogo inicial de identidad:

```bash
dotnet ef migrations add SeedIdentityCatalog \
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

## Bootstrap del Administrador Inicial

El primer administrador se crea únicamente mediante el comando explícito `--bootstrap-admin`. El inicio normal de la API no crea usuarios, no exige `InitialAdmin` y no ejecuta migraciones automáticamente.

Orden recomendado:

1. Configurar `ConnectionStrings__DefaultConnection`.
2. Aplicar las migraciones con `dotnet ef database update`.
3. Configurar las variables `InitialAdmin`.
4. Ejecutar `--bootstrap-admin`.
5. Eliminar las variables sensibles `InitialAdmin`.

Variables requeridas:

- `InitialAdmin__FirstName`
- `InitialAdmin__LastName`
- `InitialAdmin__Email`
- `InitialAdmin__Password`

PowerShell:

```powershell
$env:InitialAdmin__FirstName="Nombre"
$env:InitialAdmin__LastName="Apellido"
$env:InitialAdmin__Email="admin@institucion.edu.ar"
$env:InitialAdmin__Password="REEMPLAZAR_CON_PASSWORD_SEGURA"

dotnet run `
  --project src/AcademicSurveySystem.Api `
  -- --bootstrap-admin

Remove-Item Env:InitialAdmin__Password
Remove-Item Env:InitialAdmin__FirstName
Remove-Item Env:InitialAdmin__LastName
Remove-Item Env:InitialAdmin__Email
```

Bash:

```bash
export InitialAdmin__FirstName="Nombre"
export InitialAdmin__LastName="Apellido"
export InitialAdmin__Email="admin@institucion.edu.ar"
export InitialAdmin__Password="REEMPLAZAR_CON_PASSWORD_SEGURA"

dotnet run \
  --project src/AcademicSurveySystem.Api \
  -- --bootstrap-admin

unset InitialAdmin__Password
unset InitialAdmin__FirstName
unset InitialAdmin__LastName
unset InitialAdmin__Email
```

El comando es idempotente: si ya existe un usuario con rol `administrator`, finaliza correctamente sin crear otro administrador y sin restablecer contraseñas. Si el email configurado ya pertenece a un usuario que no es administrador, el comando falla y no eleva privilegios silenciosamente.

## Autenticación JWT

La API emite JWT firmados con HMAC SHA-256. La clave de firma no debe almacenarse en `appsettings.json`; debe configurarse mediante variable de entorno o secretos locales.

Configuración base:

```json
{
  "Jwt": {
    "Issuer": "AcademicSurveySystem",
    "Audience": "AcademicSurveySystem.Web",
    "SigningKey": "",
    "AccessTokenExpirationMinutes": 60
  }
}
```

Variables soportadas:

- `Jwt__SigningKey`: obligatoria en el inicio normal de la API; mínimo 32 caracteres.
- `Jwt__Issuer`: opcional.
- `Jwt__Audience`: opcional.
- `Jwt__AccessTokenExpirationMinutes`: opcional; mínimo 5 y máximo 1440.

PowerShell:

```powershell
$env:Jwt__SigningKey="REEMPLAZAR_CON_CLAVE_SEGURA_DE_AL_MENOS_32_CARACTERES"
```

Bash:

```bash
export Jwt__SigningKey="REEMPLAZAR_CON_CLAVE_SEGURA_DE_AL_MENOS_32_CARACTERES"
```

El modo `--bootstrap-admin` no requiere `Jwt__SigningKey`, porque no inicia el servidor web ni emite tokens.

### Login

Endpoint:

```text
POST /api/auth/login
```

Request:

```json
{
  "email": "admin@institucion.edu.ar",
  "password": "REEMPLAZAR_CON_PASSWORD_DEL_ADMINISTRADOR"
}
```

Respuesta exitosa de ejemplo, sin token real:

```json
{
  "accessToken": "TOKEN_JWT_EMITIDO",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-09-03T18:00:00Z",
  "user": {
    "id": "00000000-0000-0000-0000-000000000000",
    "firstName": "Nombre",
    "lastName": "Apellido",
    "email": "admin@institucion.edu.ar",
    "roles": ["administrator"],
    "permissions": ["identity.users.read"]
  }
}
```

Los errores de credenciales usan mensajes genéricos. Usuarios `Inactive` o `Blocked` reciben `HTTP 403` y no pueden iniciar sesión.

Si BCrypt indica `SuccessRehashNeeded`, la API actualiza el `password_hash` con el factor de trabajo vigente sin exponer el hash anterior ni el nuevo.

### Usuario Actual

Endpoint:

```text
GET /api/auth/me
```

Requiere header:

```text
Authorization: Bearer TOKEN_JWT_EMITIDO
```

Este endpoint devuelve los datos del usuario desde claims del token y no consulta la base de datos en esta etapa.

La autorización futura se realizará por permisos mediante claims `permission`, por ejemplo con `[RequirePermission("identity.users.read")]` o políticas `Permission:identity.users.read`.

Todavía no existen refresh tokens, recuperación de contraseña, cambio de contraseña, registro público ni frontend de login.

## Catálogo Académico

El backend incluye el núcleo de dominio y persistencia del catálogo académico. Ya existen endpoints protegidos, controladores y DTOs para `Career`, `AcademicCycle`, `Subject`, `Teacher` y `TeacherSubjectAssignment`. Todavía no existe frontend académico.

Entidades académicas implementadas:

- `Career`: carrera, curso o trayecto académico.
- `Subject`: materia perteneciente a una carrera.
- `Teacher`: docente con email opcional.
- `AcademicCycle`: ciclo lectivo anual o semestral.
- `TeacherSubjectAssignment`: asignación de un docente a una materia durante un ciclo lectivo.

Tablas creadas por la migración `AddAcademicCatalog`:

- `careers`: almacena código, nombre, tipo y estado activo de carreras.
- `subjects`: almacena materias por carrera, año y período.
- `teachers`: almacena docentes y email opcional normalizado.
- `academic_cycles`: almacena año, período y fechas de inicio/fin del ciclo.
- `teacher_subject_assignments`: relaciona docente, materia y ciclo lectivo con una función docente.

Las entidades usan `Guid` como identificador, `DateTimeOffset` en UTC para auditoría básica, `DateOnly` para fechas académicas del ciclo lectivo e `IsActive` para desactivación lógica. No se agregan filtros globales todavía.

Crear la migración del catálogo académico:

```bash
dotnet ef migrations add AddAcademicCatalog \
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

No existe seed académico: la migración no inserta carreras, materias, docentes, ciclos ni asignaciones reales.

## Endpoints del Catálogo Académico

Los endpoints académicos disponibles cubren `Career`, `AcademicCycle`, `Subject`, `Teacher` y `TeacherSubjectAssignment`. Todos requieren JWT válido y autorización por permisos.

Permisos requeridos:

- Lectura: `academic.catalog.read`
- Escritura: `academic.catalog.manage`

### Carreras

Ruta base:

```text
/api/academic/careers
```

Endpoints:

- `GET /api/academic/careers?includeInactive=false`
- `GET /api/academic/careers/{id}`
- `POST /api/academic/careers`
- `PUT /api/academic/careers/{id}`
- `PATCH /api/academic/careers/{id}/activate`
- `PATCH /api/academic/careers/{id}/deactivate`

Crear carrera:

```json
{
  "code": "tuds",
  "name": "Tecnicatura Universitaria en Desarrollo de Software",
  "type": "Undergraduate"
}
```

Actualizar carrera:

```json
{
  "name": "Tecnicatura Universitaria en Desarrollo de Software",
  "type": "Undergraduate"
}
```

### Ciclos Lectivos

Ruta base:

```text
/api/academic/academic-cycles
```

Endpoints:

- `GET /api/academic/academic-cycles?includeInactive=false`
- `GET /api/academic/academic-cycles/{id}`
- `POST /api/academic/academic-cycles`
- `PUT /api/academic/academic-cycles/{id}`
- `PATCH /api/academic/academic-cycles/{id}/activate`
- `PATCH /api/academic/academic-cycles/{id}/deactivate`

Crear ciclo lectivo:

```json
{
  "year": 2026,
  "period": "Annual",
  "startDate": "2026-03-01",
  "endDate": "2026-12-15"
}
```

Actualizar ciclo lectivo:

```json
{
  "period": "Annual",
  "startDate": "2026-03-01",
  "endDate": "2026-12-15"
}
```

### Materias

Ruta base:

```text
/api/academic/subjects
```

Endpoints:

- `GET /api/academic/subjects?includeInactive=false&careerId=`
- `GET /api/academic/subjects/{id}`
- `POST /api/academic/subjects`
- `PUT /api/academic/subjects/{id}`
- `PATCH /api/academic/subjects/{id}/activate`
- `PATCH /api/academic/subjects/{id}/deactivate`

Crear materia:

```json
{
  "careerId": "00000000-0000-0000-0000-000000000000",
  "code": "programacion-i",
  "name": "Programación I",
  "year": 1,
  "period": "FirstSemester"
}
```

Actualizar materia:

```json
{
  "name": "Programación I",
  "year": 1,
  "period": "FirstSemester"
}
```

### Docentes

Ruta base:

```text
/api/academic/teachers
```

Endpoints:

- `GET /api/academic/teachers?includeInactive=false`
- `GET /api/academic/teachers/{id}`
- `POST /api/academic/teachers`
- `PUT /api/academic/teachers/{id}`
- `PATCH /api/academic/teachers/{id}/activate`
- `PATCH /api/academic/teachers/{id}/deactivate`

Crear docente:

```json
{
  "firstName": "Juan",
  "lastName": "Pérez",
  "email": "juan.perez@institucion.edu.ar"
}
```

Actualizar docente:

```json
{
  "firstName": "Juan",
  "lastName": "Pérez",
  "email": "juan.perez@institucion.edu.ar"
}
```

El email es opcional. Para quitarlo, enviar `null` o un string vacío.

### Asignaciones Docente-Materia-Ciclo

Ruta base:

```text
/api/academic/teacher-subject-assignments
```

Endpoints:

- `GET /api/academic/teacher-subject-assignments?includeInactive=false&teacherId=&subjectId=&academicCycleId=`
- `GET /api/academic/teacher-subject-assignments/{id}`
- `POST /api/academic/teacher-subject-assignments`
- `PUT /api/academic/teacher-subject-assignments/{id}`
- `PATCH /api/academic/teacher-subject-assignments/{id}/activate`
- `PATCH /api/academic/teacher-subject-assignments/{id}/deactivate`

Crear asignación:

```json
{
  "teacherId": "00000000-0000-0000-0000-000000000000",
  "subjectId": "00000000-0000-0000-0000-000000000000",
  "academicCycleId": "00000000-0000-0000-0000-000000000000",
  "teachingRole": "Titular"
}
```

Actualizar asignación:

```json
{
  "teachingRole": "Adjunto"
}
```

Los endpoints devuelven `400` para errores de validación, `404` si el recurso no existe, `409` ante conflictos de unicidad y `500` para fallos no esperados sin exponer stack traces.

Todavía no existe frontend académico.

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

Infraestructura inicial de persistencia configurada. El núcleo persistente de identidad ya existe con `User`, `Role`, `Permission`, `UserRole` y `RolePermission`, más un catálogo inicial de cuatro roles y catorce permisos. Existe un comando explícito e idempotente para crear el primer administrador con contraseña hasheada. La API ya cuenta con login básico, emisión de JWT, endpoint protegido `/api/auth/me` y autorización por permisos. El dominio académico ya incluye carreras, materias, docentes, ciclos lectivos y asignaciones docente-materia-ciclo con persistencia EF Core. Ya existen endpoints académicos protegidos para `Career`, `AcademicCycle`, `Subject`, `Teacher` y `TeacherSubjectAssignment`. Todavía no existe frontend académico, encuestas, sesiones QR, respuestas ni reportes.
