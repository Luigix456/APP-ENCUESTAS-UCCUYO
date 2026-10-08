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
- QuestPDF 2026.9.1
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

## Seed catálogo académico UCCuyo

El catálogo institucional de UCCuyo se carga con un comando explícito e idempotente. Inserta únicamente unidades académicas y carreras/trayectos académicos faltantes: no crea materias, docentes, ciclos lectivos ni matrículas.

El total esperado del catálogo estático es:

- 9 unidades académicas.
- 62 carreras/trayectos académicos.

El seeder busca primero por `Code` normalizado y luego evita duplicados por nombre cuando corresponde. No borra registros existentes, no reemplaza IDs, no renombra carreras existentes y preserva datos ya vinculados, como la carrera `tuds` si ya existe.

PowerShell:

```powershell
dotnet run `
  --project src/AcademicSurveySystem.Api `
  -- --seed-uccuyo-academic-catalog
```

Bash:

```bash
dotnet run \
  --project src/AcademicSurveySystem.Api \
  -- --seed-uccuyo-academic-catalog
```

Antes de ejecutarlo, configurar `ConnectionStrings__DefaultConnection` y aplicar las migraciones existentes. El comando no ejecuta migraciones automáticamente y no requiere credenciales JWT.

## Reset de contraseña de administrador

El comando `--reset-admin-password` permite restablecer la contraseña de un usuario administrador existente. No crea usuarios nuevos, no asigna roles, no modifica permisos, no emite JWT y no ejecuta migraciones automáticamente.

Variables requeridas:

- `AdminPasswordReset__Email`: email del usuario administrador existente.
- `AdminPasswordReset__NewPassword`: nueva contraseña, validada con la misma política usada para el administrador inicial.

PowerShell:

```powershell
$env:AdminPasswordReset__Email="admin@institucion.edu.ar"
$env:AdminPasswordReset__NewPassword="REEMPLAZAR_CON_PASSWORD_SEGURA"

dotnet run `
  --project src/AcademicSurveySystem.Api `
  -- --reset-admin-password

Remove-Item Env:AdminPasswordReset__NewPassword
Remove-Item Env:AdminPasswordReset__Email
```

Bash:

```bash
export AdminPasswordReset__Email="admin@institucion.edu.ar"
export AdminPasswordReset__NewPassword="REEMPLAZAR_CON_PASSWORD_SEGURA"

dotnet run \
  --project src/AcademicSurveySystem.Api \
  -- --reset-admin-password

unset AdminPasswordReset__NewPassword
unset AdminPasswordReset__Email
```

Advertencias de seguridad:

- Ejecutar el comando sólo desde un entorno administrativo confiable.
- No escribir contraseñas reales en documentación, commits, tickets, capturas ni historial compartido.
- El comando no imprime la contraseña ni el hash.
- Si el usuario no existe o no tiene rol `administrator`, falla sin modificar la contraseña.
- El comando es independiente de `--bootstrap-admin`; no deben ejecutarse ambos flags en la misma invocación.

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

## Administración de Usuarios

La API permite administrar usuarios internos desde endpoints protegidos. La creación usa la misma política de contraseña del administrador inicial, almacena únicamente hash BCrypt y nunca devuelve contraseñas ni hashes en los DTOs.

Ruta base de usuarios:

```text
/api/identity/users
```

Endpoints:

- `GET /api/identity/users?includeInactive=false`
- `GET /api/identity/users/{userId}`
- `POST /api/identity/users`
- `PUT /api/identity/users/{userId}`
- `PATCH /api/identity/users/{userId}/activate`
- `PATCH /api/identity/users/{userId}/deactivate`
- `PUT /api/identity/users/{userId}/roles`

Permisos requeridos:

- Listar y consultar usuarios: `identity.users.read`
- Crear usuarios: `identity.users.create`
- Actualizar datos básicos y activar/desactivar: `identity.users.update`
- Reemplazar roles: `identity.users.assign_roles`

Ejemplo de creación:

```json
{
  "firstName": "Maria",
  "lastName": "Gomez",
  "email": "director.tuds@institucion.edu.ar",
  "password": "REEMPLAZAR_CON_PASSWORD_SEGURA",
  "roleIds": [
    "44444444-4444-4444-4444-444444444444"
  ]
}
```

El endpoint de actualización básica sólo modifica `firstName`, `lastName` y `email`; no modifica contraseña ni roles. La asignación de roles se realiza con reemplazo completo:

```json
{
  "roleIds": [
    "44444444-4444-4444-4444-444444444444"
  ]
}
```

Un usuario desactivado no puede iniciar sesión, pero conserva historial, roles y asociaciones `UserCareer`. No existe eliminación física de usuarios.

## Roles de Identidad

Ruta:

```text
/api/identity/roles
```

Endpoint:

- `GET /api/identity/roles`

Permiso requerido:

- `identity.roles.read`

La respuesta incluye roles activos y sus permisos asociados. Los roles son los definidos por seed; no existen roles dinámicos ni edición administrativa de permisos en esta etapa.

## Asociación de Usuarios y Carreras

El backend permite asociar usuarios existentes con carreras existentes mediante una relación explícita muchos-a-muchos.

Tabla configurada:

- `user_careers`

Campos principales:

- `user_id`
- `career_id`
- `assigned_at_utc`

La clave primaria es compuesta por `user_id` y `career_id`. Las relaciones hacia `users` y `careers` usan eliminación restringida: eliminar una asociación no elimina el usuario ni la carrera, y desactivar una carrera no elimina la asociación.

La asignación valida que el usuario exista y esté activo, que cada carrera exista y esté activa, y que no haya carreras duplicadas en el request. Esta relación no está acoplada al rol ni a la idea de `career_director`, pero puede combinarse con el rol `career_director` para probar alcance `results.read_career`.

Ruta base:

```text
/api/identity/users/{userId}/careers
```

Permisos requeridos:

- Lectura: `identity.users.read`
- Escritura: `identity.users.update`

Endpoints:

- `GET /api/identity/users/{userId}/careers`
- `PUT /api/identity/users/{userId}/careers`

Ejemplo de actualización:

```json
{
  "careerIds": [
    "22222222-2222-2222-2222-222222222222",
    "33333333-3333-3333-3333-333333333333"
  ]
}
```

## Documentación Swagger/OpenAPI

Swagger está habilitado para desarrollo y pruebas en el entorno `Development`.

URL local esperada:

```text
/swagger
```

Para probar endpoints protegidos desde Swagger:

1. Ejecutar `POST /api/auth/login`.
2. Copiar el valor `accessToken` de la respuesta.
3. Usar el botón `Authorize` en Swagger.
4. Escribir el token con el formato `Bearer TOKEN_JWT`.

No se deben pegar tokens reales, credenciales ni secretos en documentación, commits, issues o capturas compartidas. Swagger está orientado a validación local y pruebas de desarrollo.

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

## Encuestas Dinámicas

El backend incluye el núcleo de dominio y persistencia para encuestas dinámicas. Ya existen endpoints protegidos, controladores, DTOs y servicios de aplicación para administrar plantillas de encuestas, sus secciones, preguntas, opciones y filas de matriz. También existe la estructura backend para asignar encuestas publicadas a contextos académicos y crear sesiones temporales con `accessCode` para acceso público. Todavía no existen respuestas públicas, resultados ni reportes de encuestas.

Entidades implementadas:

- `Survey`: plantilla principal de encuesta.
- `SurveySection`: sección ordenada dentro de una encuesta.
- `SurveyQuestion`: pregunta dinámica dentro de una sección.
- `SurveyQuestionOption`: opción manual para preguntas de selección.
- `SurveyMatrixRow`: fila para preguntas de matriz.
- `SurveyAssignment`: asignación de una plantilla publicada a carrera, materia, ciclo académico y asignación docente-materia.
- `SurveySession`: sesión temporal asociada a una asignación de encuesta.

Enumeraciones implementadas:

- `SurveyStatus`: `Draft`, `Published`, `Archived`.
- `SurveyTarget`: `Student`, `Teacher`, `Institutional`.
- `SurveyQuestionType`: `SingleChoice`, `MultipleChoice`, `ShortText`, `LongText`, `RatingScale`, `MatrixSingleChoice`.
- `SurveySessionStatus`: `Created`, `Open`, `Closed`, `Cancelled`, `Expired`.

Tablas creadas por la migración `AddSurveyCore`:

- `surveys`
- `survey_sections`
- `survey_questions`
- `survey_question_options`
- `survey_matrix_rows`

Crear la migración del núcleo de encuestas:

```bash
dotnet ef migrations add AddSurveyCore \
  --project src/AcademicSurveySystem.Infrastructure \
  --startup-project src/AcademicSurveySystem.Api \
  --output-dir Persistence/Migrations
```

La migración no inserta seed de encuestas y no crea respuestas ni reportes.

## Endpoints de Plantillas de Encuestas

Ruta base:

```text
/api/surveys
```

Todos los endpoints requieren JWT válido y autorización por permisos.

Permisos requeridos:

- Lectura: `surveys.templates.read`
- Escritura: `surveys.templates.manage`

Endpoints de encuesta:

- `GET /api/surveys?includeInactive=false&status=&target=`
- `GET /api/surveys/{id}`
- `POST /api/surveys`
- `PUT /api/surveys/{id}`
- `PATCH /api/surveys/{id}/publish`
- `PATCH /api/surveys/{id}/archive`
- `PATCH /api/surveys/{id}/activate`
- `PATCH /api/surveys/{id}/deactivate`

Endpoints de secciones:

- `POST /api/surveys/{surveyId}/sections`
- `PUT /api/surveys/{surveyId}/sections/{sectionId}`
- `PATCH /api/surveys/{surveyId}/sections/{sectionId}/activate`
- `PATCH /api/surveys/{surveyId}/sections/{sectionId}/deactivate`

Endpoints de preguntas:

- `POST /api/surveys/{surveyId}/sections/{sectionId}/questions`
- `PUT /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}`
- `PATCH /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}/activate`
- `PATCH /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}/deactivate`

Endpoints de opciones:

- `POST /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}/options`
- `PATCH /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}/options/{optionId}/activate`
- `PATCH /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}/options/{optionId}/deactivate`

Endpoints de filas de matriz:

- `POST /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}/matrix-rows`
- `PATCH /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}/matrix-rows/{rowId}/activate`
- `PATCH /api/surveys/{surveyId}/sections/{sectionId}/questions/{questionId}/matrix-rows/{rowId}/deactivate`

La publicación valida que la encuesta no esté archivada, tenga al menos una sección activa, que cada sección activa tenga al menos una pregunta activa y que las preguntas de selección o matriz tengan las opciones y filas activas mínimas requeridas.

## Asignaciones de Encuestas

Las asignaciones permiten asociar una plantilla de encuesta publicada y activa a un contexto académico específico compuesto por carrera, materia, ciclo académico y asignación docente-materia-ciclo.

Tabla configurada:

- `survey_assignments`

Relaciones configuradas con eliminación restringida:

- `survey_id` -> `surveys`
- `career_id` -> `careers`
- `subject_id` -> `subjects`
- `academic_cycle_id` -> `academic_cycles`
- `teacher_subject_assignment_id` -> `teacher_subject_assignments`

La combinación `survey_id`, `career_id`, `subject_id`, `academic_cycle_id` y `teacher_subject_assignment_id` es única.

Ruta base:

```text
/api/survey-assignments
```

Todos los endpoints requieren JWT válido y autorización por permisos existentes.

Permisos requeridos:

- Lectura: `surveys.templates.read`
- Escritura: `surveys.templates.manage`

Endpoints:

- `GET /api/survey-assignments?includeInactive=false&surveyId=&careerId=&subjectId=&academicCycleId=&teacherId=`
- `GET /api/survey-assignments/{id}`
- `POST /api/survey-assignments`
- `PATCH /api/survey-assignments/{id}/activate`
- `PATCH /api/survey-assignments/{id}/deactivate`

Ejemplo de creación:

```json
{
  "surveyId": "11111111-1111-1111-1111-111111111111",
  "careerId": "22222222-2222-2222-2222-222222222222",
  "subjectId": "33333333-3333-3333-3333-333333333333",
  "academicCycleId": "44444444-4444-4444-4444-444444444444",
  "teacherSubjectAssignmentId": "55555555-5555-5555-5555-555555555555"
}
```

Todavía no existe frontend académico para administración completa. El backend no genera todavía imágenes QR binarias.

## Sesiones de Encuesta y QR Temporal

`SurveySession` representa una sesión temporal para responder una encuesta publicada dentro de un contexto académico previamente definido por `SurveyAssignment`.

Cada sesión genera un `accessCode` único y URL-safe. Ese código se usa para construir:

- `publicPath`: ruta pública relativa.
- `publicUrl`: URL pública completa, opcional si el entorno permite construirla.

El QR debe apuntar a `publicUrl` o, si no está disponible, a `publicPath`. El backend todavía no genera una imagen QR binaria ni agrega librerías externas para QR.

El acceso público sólo es válido cuando la sesión está `Open`, activa y no vencida. Cerrar, cancelar, desactivar o vencer la sesión invalida el QR.

Tabla configurada:

- `survey_sessions`

Relaciones configuradas con eliminación restringida:

- `survey_assignment_id` -> `survey_assignments`
- `created_by_user_id` -> `users`

Índices configurados:

- único por `access_code`
- por `survey_assignment_id`
- por `created_by_user_id`
- por `status`
- por `expires_at_utc`

Permiso requerido para gestión:

- `surveys.sessions.manage`

Endpoints protegidos:

- `GET /api/survey-sessions?includeInactive=false&status=&surveyAssignmentId=&accessCode=`
- `GET /api/survey-sessions/{id}`
- `POST /api/survey-sessions`
- `PUT /api/survey-sessions/{id}`
- `PATCH /api/survey-sessions/{id}/open`
- `PATCH /api/survey-sessions/{id}/close`
- `PATCH /api/survey-sessions/{id}/cancel`
- `PATCH /api/survey-sessions/{id}/activate`
- `PATCH /api/survey-sessions/{id}/deactivate`

Endpoint público:

- `GET /api/public/survey-sessions/{accessCode}`

Ejemplo de creación:

```json
{
  "surveyAssignmentId": "66666666-6666-6666-6666-666666666666",
  "title": "Evaluación Programación I - Aula 3",
  "location": "Aula 3",
  "expiresAtUtc": "2026-09-07T22:00:00Z"
}
```

Ejemplo de respuesta administrativa:

```json
{
  "id": "77777777-7777-7777-7777-777777777777",
  "surveyAssignmentId": "66666666-6666-6666-6666-666666666666",
  "accessCode": "codigo-url-safe",
  "publicPath": "/api/public/survey-sessions/codigo-url-safe",
  "publicUrl": null,
  "title": "Evaluación Programación I - Aula 3",
  "location": "Aula 3",
  "status": "Created",
  "expiresAtUtc": "2026-09-07T22:00:00Z",
  "isActive": true
}
```

Ejemplo de respuesta pública:

```json
{
  "sessionId": "77777777-7777-7777-7777-777777777777",
  "accessCode": "codigo-url-safe",
  "expiresAtUtc": "2026-09-07T22:00:00Z",
  "surveyId": "11111111-1111-1111-1111-111111111111",
  "surveyTitle": "Encuesta de cursada",
  "surveyDescription": "Encuesta anónima para estudiantes",
  "surveyTarget": "Student",
  "careerName": "Tecnicatura Superior en Desarrollo de Software",
  "subjectName": "Programación I",
  "academicCycleYear": 2026,
  "academicCyclePeriod": "Annual",
  "teacherFullName": "Ada Lovelace",
  "teachingRole": "Titular",
  "sections": []
}
```

## Alcance de resultados por carrera

Los endpoints protegidos de resultados respetan el alcance del usuario autenticado:

- `results.read_all`: permite consultar resultados de todas las carreras.
- `results.read_career`: permite consultar sólo resultados cuyo `SurveyAssignment.CareerId` esté asociado al usuario autenticado en `user_careers`.

La autorización usa el identificador del usuario desde el JWT y valida el acceso antes de consultar el resultado agregado. Si el usuario tiene `results.read_career` pero no tiene asociación con la carrera de la asignación o sesión consultada, la API responde `HTTP 403`.

Este alcance aplica a resultados por `SurveyAssignment`, por `SurveySession` y a la vista previa de reportes. No modifica roles, permisos, JWT, plantillas de encuesta, asignaciones académicas ni lógica de publicación.

## Reportes de Resultados

El backend expone reportes institucionales de resultados a partir de los datos agregados existentes. La capa de reportes compone:

- metadata institucional;
- metadata académica de la asignación;
- versión real de la encuesta (`Survey.VersionNumber`);
- resumen de respuestas/sesiones;
- resultados por pregunta ya calculados por el módulo de resultados.

Endpoints:

- `GET /api/reports/survey-assignments/{surveyAssignmentId}`
- `GET /api/reports/survey-assignments/{surveyAssignmentId}/pdf`

Permisos de preview JSON:

- `results.read_all`, o
- `results.read_career` dentro del alcance de carreras asociado al usuario en `user_careers`.

Permisos de exportación PDF:

- `reports.export`, y además
- `results.read_all`, o
- `results.read_career` dentro del alcance de carreras asociado al usuario en `user_careers`.

`reports.export` por sí solo no permite acceder a resultados ni exportar reportes fuera del scope de carrera. La autorización reutiliza `ResultsAccessService` y se valida antes de construir el reporte.

El preview devuelve `SurveyReportDto` en JSON e incluye institución, encuesta, versión, carrera, materia, docente, rol docente, ciclo lectivo, totales, fechas de primera/última respuesta y preguntas con resultados agregados.

El PDF se genera server-side on-demand con QuestPDF. No se guarda en PostgreSQL, no se guarda en disco y no crea tablas nuevas. El PDF usa formato A4 vertical, encabezado institucional, resumen, gráficos vectoriales simples, respuestas abiertas, comentarios anónimos, footer con fecha UTC y paginación.

La configuración no sensible de institución se lee desde:

```json
{
  "Reports": {
    "InstitutionName": "Universidad Católica de Cuyo",
    "SystemName": "Sistema Web de Gestión de Encuestas Académicas",
    "FacultyName": "Facultad de Ciencias Económicas y Empresariales"
  }
}
```

Las respuestas HTTP de reportes usan `Cache-Control: private, no-store` porque pueden contener información académica sensible. El DTO y el PDF no incluyen estudiantes, identidad de respondentes, emails de respondentes, IP, fingerprint, accessCode, JWT, contraseñas ni hashes.

QuestPDF se configura explícitamente con `LicenseType.Community`. La licencia Community es gratuita sólo para los casos permitidos por QuestPDF, incluyendo individuos, organizaciones sin fines de lucro, proyectos FOSS y organizaciones bajo el umbral de ingresos indicado por la licencia vigente. Si el despliegue no califica, debe adquirirse una licencia comercial antes de usarlo en producción.

## Auditoría de Cambios

El sistema registra eventos administrativos relevantes en la tabla append-only `audit_entries`. No existen endpoints `PUT`, `PATCH` ni `DELETE` para modificar o borrar auditoría.

Eventos auditados:

- Identidad: creación/actualización de usuarios, activación/desactivación, restablecimiento de contraseña, cambios de roles y carreras asociadas.
- Catálogo académico: creación/actualización/activación/desactivación de unidades, carreras, ciclos, materias, docentes, asignaciones docente-materia y matrícula.
- Encuestas: creación, actualización, publicación, archivo, activación/desactivación y creación de nuevas versiones de plantillas.
- Asignaciones y sesiones: creación/activación/desactivación de asignaciones, creación/apertura/cierre/cancelación/activación/desactivación de sesiones.
- Reportes: exportación PDF de informes.

No se auditan respuestas públicas de estudiantes ni contenido sensible como respuestas, comentarios, `OtherText`, contraseñas, hashes, JWT, headers `Authorization`, `accessCode`, payloads QR, IP/fingerprint de estudiantes, cadenas de conexión, secretos, tokens de reset ni claves internas.

Endpoint:

```text
GET /api/audit
```

Permiso requerido:

- `audit.read`

Filtros soportados:

- `fromUtc`
- `toUtc`
- `actorUserId`
- `module`
- `action`
- `entityType`
- `search`
- `page`
- `pageSize`

La consulta devuelve eventos ordenados por `occurredAtUtc` descendente e `id` descendente, con paginación server-side. `audit.read` permite auditoría institucional completa; no aplica alcance por `UserCareer`.

## Ejecutar Frontend

```bash
cd frontend/academic-survey-system-web
npm install
npm run dev
```

## Estado Actual

Infraestructura inicial de persistencia configurada. El núcleo persistente de identidad ya existe con `User`, `Role`, `Permission`, `UserRole` y `RolePermission`, más un catálogo inicial de cuatro roles y catorce permisos. Existe un comando explícito e idempotente para crear el primer administrador con contraseña hasheada. La API ya cuenta con login básico, emisión de JWT, endpoint protegido `/api/auth/me`, autorización por permisos, administración protegida de usuarios, activación/desactivación, reemplazo de roles y consulta de roles disponibles. El dominio académico ya incluye carreras, materias, docentes, ciclos lectivos y asignaciones docente-materia-ciclo con persistencia EF Core. Ya existen endpoints académicos protegidos para `Career`, `AcademicCycle`, `Subject`, `Teacher` y `TeacherSubjectAssignment`. También existen asociaciones protegidas entre usuarios y carreras, endpoints protegidos para administrar plantillas de encuestas dinámicas, asignarlas a contextos académicos, gestionar sesiones temporales de encuesta con `accessCode`, consultar resultados con alcance por permisos, generar reportes JSON/PDF on-demand y consultar auditoría con `audit.read`. Existe un endpoint público para consultar una sesión abierta, activa y vigente sin JWT. Todavía no existe importación CSV/XLSX, comparación histórica, ayuda contextual ni tokens anónimos. El backend todavía no genera imágenes QR binarias.
