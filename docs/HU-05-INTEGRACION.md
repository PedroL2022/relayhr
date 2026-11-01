# HU-05 — Revisión y publicación de Vacantes Express

Repositorio: `C:\Users\User Name\Desktop\GIT\relayhr`  
Rama: `JoscarDevImplementation`  
Fecha: 1 de noviembre de 2026.

## Estado y alcance

La implementación de HU-05 está integrada en el repositorio y verificada con pruebas relacionales y HTTP. La solución compila con 0 errores y 0 advertencias. **La puesta en servicio sobre SQL Server queda pendiente**: no se pudo inspeccionar la instancia LocalDB bajo la cuenta de ejecución y no se aplicaron migraciones ni se modificaron datos reales. No se afirma una validación completa sobre una base SQL Server existente.

Se reutilizan entidad, estados, generación, interceptor, worker, contexto y consultas de HU-04. No se creó otro módulo ni proyecto. No se modificaron freelancers, solicitudes de servicio ni transferencia de conocimiento.

## A. Archivos modificados o creados

Rutas relativas a la raíz indicada arriba. El Designer histórico duplicado permanece en disco; se excluye solamente su copia accidental `.cs.cs` de la compilación.

| Archivo | Cambio |
|---|---|
| `src/RelayHR.Web/Controllers/AuthController.cs` | Alias de User para corregir la ambigüedad previa entre Identity y freelancers. |
| `src/RelayHR.Web/Controllers/ExpressVacanciesController.cs` | Extiende la API de HU-04 con edición, publicación, departamentos y auditoría en consultas. |
| `src/RelayHR.Web/Data/AppDbContext.cs` | Mantiene el contrato de HU-04 compartiendo el modelo canónico de RelayHrDbContext. |
| `src/RelayHR.Web/Data/Configurations/DepartmentConfiguration.cs` | Corrige la clave al nombre canónico Id. |
| `src/RelayHR.Web/Data/Configurations/EmployeeConfiguration.cs` | Configura Id/Name y referencias opcionales a puesto y departamento. |
| `src/RelayHR.Web/Data/Configurations/EmployeeExitConfiguration.cs` | Corrige las propiedades incompatibles de HU-04 a Id/EffectiveDate. |
| `src/RelayHR.Web/Data/Configurations/ExpressVacancyConfiguration.cs` | Estado Published, token de concurrencia, auditoría y claves foráneas a Identity; JSON portable. |
| `src/RelayHR.Web/Data/Migrations/RelayHrDbContextModelSnapshot.cs` | Snapshot actualizado del modelo canónico de salidas, Identity y vacantes. |
| `src/RelayHR.Web/Data/RelayHrDbContext.cs` | Incluye puestos, vacantes y solicitudes de generación, reutilizando sus configuraciones. |
| `src/RelayHR.Web/Dtos/ExpressVacancyResponseDTO.cs` | Amplía el detalle interno con departamento, auditoría y métricas de publicación. |
| `src/RelayHR.Web/Entities/Employee.cs` | Referencias opcionales necesarias para copiar el perfil del puesto de HU-04. |
| `src/RelayHR.Web/Entities/EmployeeExit.cs` | Navegación inversa a su única Vacante Express. |
| `src/RelayHR.Web/Entities/ExpressVacancy.cs` | Auditoría con IDs int, revisión Guid y detección de marcadores pendientes. |
| `src/RelayHR.Web/Entities/ExpressVacancyStatus.cs` | Agrega Published sin renumerar estados existentes. |
| `src/RelayHR.Web/Program.cs` | Registra publicación, conecta el interceptor a salidas, mantiene cookies y configura JWT/políticas/CSRF. |
| `src/RelayHR.Web/RelayHR.Web.csproj` | Alinea net10.0 con dependencias existentes, añade JWT y excluye el Designer duplicado de compilación. |
| `src/RelayHR.Web/Security/AuthConstants.cs` | Política específica PublishVacancy. |
| `src/RelayHR.Web/Services/ExpressVacancyService.cs` | Consulta la salida con su clave real Id, conservando la generación existente. |
| `src/RelayHR.Web/Services/ExpressVacancyWorker.cs` | Corrige el nombre de la API de PeriodicTimer; no reescribe el worker. |
| `src/RelayHR.Web/Views/Shared/_Layout.cshtml` | Enlace de navegación a vacantes para los roles autorizados. |
| `src/RelayHR.Web/wwwroot/css/site.css` | Estilos acotados a las nuevas pantallas y adaptación móvil. |
| `tests/RelayHR.Web.Tests/Integration/TestApplication.cs` | SQLite compartido entre contextos, interceptor real, cookies/JWT y servicios de prueba aislados. |
| `src/RelayHR.Web/Controllers/PublicVacanciesController.cs` | Consulta anónima restringida a Published mediante DTO público. |
| `src/RelayHR.Web/Controllers/VacanciesController.cs` | Pantallas MVC de listado, revisión y detalle público. |
| `src/RelayHR.Web/DTOs/PublicVacancyDTO.cs` | Contrato para candidatos sin datos de empleados, salidas ni usuarios internos. |
| `src/RelayHR.Web/DTOs/UpdateExpressVacancyDTO.cs` | Campos editables, validaciones y distinción entre omitido y null explícito. |
| `src/RelayHR.Web/Data/Migrations/20261101205756_ExpressVacancyReviewPublication.Designer.cs` | Migración incremental generada por EF Core y su modelo de destino. |
| `src/RelayHR.Web/Data/Migrations/20261101205756_ExpressVacancyReviewPublication.cs` | Migración incremental generada por EF Core y su modelo de destino. |
| `src/RelayHR.Web/Data/RelayHrDbContextFactory.cs` | Permite generar y revisar migraciones sin arrancar el worker ni conectar a SQL Server. |
| `src/RelayHR.Web/Security/VacancyAntiforgeryAttribute.cs` | Exige token CSRF en operaciones de vacantes autenticadas con cookies. |
| `src/RelayHR.Web/Services/ExpressVacancyPublicationService.cs` | Reglas de edición/publicación, auditoría, validación y concurrencia optimista. |
| `src/RelayHR.Web/Views/Vacancies/Index.cshtml` | Listado paginado y filtrable de Vacantes Express. |
| `src/RelayHR.Web/Views/Vacancies/Public.cshtml` | Detalle público Razor con salida codificada y sin datos internos. |
| `src/RelayHR.Web/Views/Vacancies/Review.cshtml` | Formulario, pendientes, auditoría, métrica, confirmación y acceso al detalle público. |
| `src/RelayHR.Web/wwwroot/js/vacancies.js` | Consultas, edición, confirmación, publicación, errores específicos y control de operaciones en curso. |
| `tests/RelayHR.Web.Tests/Integration/ExpressVacancyPublicationApiTests.cs` | Pruebas HTTP, seguridad, persistencia, Razor, contrato público y flujo salida→HU-04→HU-05. |
| `tests/RelayHR.Web.Tests/Services/ExpressVacancyPublicationServiceTests.cs` | Concurrencia entre contextos independientes, límites temporales, estados y coherencia SQL Server/snapshot. |
| `docs/HU-05-INTEGRACION.md` | Este informe, decisiones, resultados e instrucciones. |

## B. Funcionalidades implementadas

- Edición limitada a PendingReview, con validación de longitudes y existencia del departamento.
- `PUT` parcial por compatibilidad con el contrato solicitado: **omitido conserva**; **null explícito limpia**. Una cadena vacía se normaliza a null. Se rechazan propiedades desconocidas o internas.
- Recalcula los cuatro campos pendientes; espacios y el marcador ToComplete no cuentan como información completa.
- Auditoría de edición y publicación mediante TimeProvider, fechas UTC e identificadores int de Identity obtenidos de la sesión/token.
- Publicación solo desde PendingReview; incompletas devuelven missingFields. Superar 48 horas no bloquea la publicación.
- Concurrencia optimista con Revision Guid. Edición y publicación actualizan el token en el mismo SaveChanges. Las escrituras competidoras reciben 409; no sobrescriben la auditoría ganadora.
- La salida original permanece intacta. Publicadas no admiten edición ni republicación.
- Consulta y vista pública solo para Published; pendientes e inexistentes responden 404 y comparten el mismo mensaje público.
- Navegación, listado, filtro, paginación, revisión, confirmación y detalle público con Razor y JavaScript existentes, sin framework adicional.
- El interceptor existente se instala también en el contexto que registra salidas. El worker de HU-04 procesa su solicitud persistida; no hay una segunda lógica de generación.

### Seguridad

Consultar/editar: HumanResources, HRAdmin o Recruiter. Publicar: HumanResources o HRAdmin. Un Recruiter no puede publicar.

Se conserva Identity.Application como esquema predeterminado de MVC. Las políticas de vacantes aceptan también JWT validado por issuer, audience, firma y vigencia. Sin una clave válida de al menos 32 caracteres, el bearer falla cerrado. El servidor toma el ID de NameIdentifier o sub; comprueba además que el usuario exista.

Las operaciones nuevas con cookie exigen antifalsificación: token generado por Razor y encabezado X-CSRF-TOKEN. Las solicitudes bearer no dependen de cookies ni requieren ese token. No se añadieron encabezados de identidad simulada.

### Medición de 48 horas

El inicio es **EmployeeExit.EffectiveDate**, no EmployeeExit.CreatedAt ni ExpressVacancy.CreatedAt. El campo existente es DateOnly: por su precisión de día se normaliza explícitamente a **00:00 UTC**, sin afirmar que se conoce la hora real del evento. La pantalla informa esta convención.

HoursToPublication = PublishedAt − fecha efectiva normalizada. Meets48HourTarget es verdadero únicamente para duraciones no negativas menores a 48 horas; a las 48 horas exactas es falso. Si se publica antes de la fecha efectiva, PublishedBeforeEffectiveExit es verdadero y duración/cumplimiento son null; se muestra que el plazo no había comenzado. La publicación sigue permitida.

## C. Endpoints

| Método y ruta | Acceso | Respuestas principales |
|---|---|---|
| GET `/api/express-vacancies` | RR. HH./reclutamiento | 200; filtro status y paginación existentes; 400 para parámetros inválidos. |
| GET `/api/express-vacancies/{id}` | RR. HH./reclutamiento | 200 con detalle, auditoría y métricas; 404. |
| GET `/api/express-vacancies/departments` | RR. HH./reclutamiento | 200 con id y nombre de departamentos. |
| PUT `/api/express-vacancies/{id}` | RR. HH./reclutamiento | 200; 400 validación/CSRF; 401; 403; 404; 409 estado/concurrencia. |
| POST `/api/express-vacancies/{id}/publish` | HumanResources/HRAdmin | 200; 400 con missingFields o error de validación; 401; 403; 404; 409. |
| GET `/api/public/vacancies/{id}` | Anónimo | 200 solo Published; 404 en otro caso. |

Se reutiliza ApiResponse del módulo de vacantes. La respuesta pública contiene únicamente id, jobTitle, department, jobDescription, requirements y publishedAt.

Ejemplo de actualización parcial:

```json
{ "jobDescription": "Coordinar las operaciones del área", "requirements": "Experiencia en coordinación" }
```

## D. Interfaz

- `/Vacancies`: bandeja con filtro por estado y paginación.
- `/Vacancies/Review/{id}`: revisión, edición, campos pendientes y publicación.
- `/vacancies/{id}`: detalle público, disponible únicamente después de publicar.

La navegación muestra “Vacantes Express” a los roles autorizados. El formulario incluye etiquetas, límites de longitud, mensajes en español, indicadores de operación, conservación de entradas ante errores y confirmación con dialog accesible. La UI bloquea acciones durante una operación y después de publicar; el servidor aplica las mismas restricciones y resuelve carreras concurrentes.

Las vistas fueron compiladas y ejercitadas por HTTP; el JavaScript pasó node --check. **No se realizó una sesión visual/interactiva en un navegador**, por lo que no se presenta ese control como ejecutado.

## E. Base de datos

Migración generada: `20261101205756_ExpressVacancyReviewPublication`, contexto **RelayHrDbContext**. No aplicada.

El contexto canónico de salidas todavía no contenía las tablas de HU-04. Por eso esta migración agrega las referencias opcionales de empleados a puesto/departamento e incorpora Positions, ExpressVacancyRequests y ExpressVacancies con su auditoría, concurrencia, índices y relaciones. Su Up no borra ni recrea tablas existentes.

AppDbContext conserva el contrato usado por los servicios de HU-04, heredando el mismo modelo de RelayHrDbContext. Las migraciones nuevas de este flujo deben usar RelayHrDbContext. No ejecutar además las cadenas históricas de AppDbContext sobre la misma base.

**Bases con HU-04 anterior ya aplicado:** el historial antiguo de AppDbContext usa EmployeeId/EmployeeExitId/DepartmentId como claves, mientras el modelo canónico usa Id. Antes de aplicar el script debe inspeccionarse el esquema y el historial real. Si esas tablas ya existen, esta migración no debe aplicarse tal cual: se necesita una reconciliación específica que conserve datos. No se automatizó ni se simuló esa conversión. Tampoco se intentó combinar las tablas Users de freelancers con Identity.

Se generó un script SQL idempotente desde la última migración de salidas. Es para revisión del esquema canónico; idempotencia por historial no convierte esquemas legacy incompatibles. Snapshot y modelo SQL Server resultaron coherentes mediante EF y una prueba automatizada. La herramienta EF disponible fue 10.0.5 y mostró aviso de versión respecto al runtime 10.0.14.

## F. Validación ejecutada

| Comprobación | Resultado |
|---|---|
| dotnet build RelayHR.slnx --no-restore | Correcto: 0 errores y 0 advertencias. |
| RelayHR.Web.Tests, suite final | **50 ejecutadas, 50 aprobadas, 0 fallidas, 0 omitidas**: 15 existentes y 35 nuevas de HU-05. |
| Métrica con fecha de creación distinta de fecha efectiva | 4 casos adicionales ejecutados después del ajuste: todos aprobados. Son una repetición dirigida, no casos nuevos del total de 50. |
| SQL Server: has-pending-model-changes | Ningún cambio pendiente respecto al snapshot. |
| SQL Server: generación de script | Correcta, sin conexión ni aplicación a una base. |
| Concurrencia | SQLite en archivo, dos DbContext/conexiones independientes, barrera de escritura: un ganador y un 409. Incluye edición frente a publicación. |
| node --check vacancies.js | Correcto. |
| git diff --check | Correcto. |
| Ejecución real sobre SQL Server | No ejecutada: LocalDB no permite acceder a su configuración bajo esta cuenta. |

Pruebas adicionales fuera de la solución:

| Proyecto | Ejecutadas | Aprobadas | Fallidas |
|---|---:|---:|---:|
| RelayHR.KnowledgeTransfer.Tests | 33 | 2 | 31 |
| RelayHR.Tests | 40 | 16 | 24 |

**El repositorio completo no tiene todas sus pruebas aprobadas.** Transferencia falla por relaciones no configuradas que arrastran entidades de salidas/Identity en su contexto provisional. Solicitudes de servicio presenta fallos de integración y errores de acceso al Event Log de Windows que enmascaran parte del diagnóstico; su servicio sigue sin registrarse en Program.cs. Estos módulos no se modificaron. No hubo una ejecución verde de referencia antes del trabajo: la compilación inicial quedó bloqueada en restore, de modo que no se atribuye automáticamente cada fallo externo a una causa nueva o antigua.

La compilación inicial intentó NuGet y falló por TLS. Para validar se usó un puente local temporal con descargas HTTPS verificadas desde Python y cachés en el directorio de trabajo. No se alteró la configuración NuGet del repositorio ni se deshabilitó la validación TLS externa.

## G. Problemas previos corregidos y pendientes

Correcciones indispensables: net9.0 se alineó a net10.0 ya usado por paquetes/tests; se añadieron el paquete JWT y los imports faltantes; se resolvió la ambigüedad User en login; se corrigieron propiedades inexistentes de HU-04, el Designer duplicado de compilación y WaitForNextTickAsync; se conectaron salidas y generación con el modelo canónico.

Pendientes fuera del alcance: integración de transferencia y solicitudes, usuarios/tablas superpuestos de freelancers, cadenas históricas de contextos y suites externas fallidas. No se rediseñó la autenticación general, ni el checklist, ni el worker, ni las pantallas de otras historias.

Pendientes de puesta en servicio: revisar/aplicar la migración en SQL Server de desarrollo, confirmar el esquema/historial si hay datos HU-04 anteriores, configurar usuarios responsables y conexión, y realizar comprobación visual/manual de escritorio y móvil. No se declara cerrada la puesta en servicio mientras estos puntos no estén verificados.

## H. Ejecución y verificación manual

Desde la raíz del repositorio, con SDK .NET 10:

```powershell
dotnet restore RelayHR.slnx
dotnet build RelayHR.slnx
dotnet test tests/RelayHR.Web.Tests/RelayHR.Web.Tests.csproj
```

1. Configurar ConnectionStrings:DefaultConnection a una base SQL Server **de desarrollo**. Antes de migrar, revisar el script y la advertencia sobre esquemas legacy del apartado E. No se ejecutan migraciones automáticamente al iniciar.
2. En una base canónica compatible, generar/revisar la migración con `dotnet ef migrations script --context RelayHrDbContext --project src/RelayHR.Web --idempotent`. Aplicarla a desarrollo solo después de comprobar su impacto y respaldo. No aplicar las migraciones de ambos contextos sobre las mismas tablas.
3. Usar una cuenta existente con HumanResources o HRAdmin. El bootstrap existente admite Identity:BootstrapUser:Name, Email y Password mediante configuración/variables de entorno; no se añadió ninguna contraseña predeterminada. El rol HumanResources se crea mediante las migraciones existentes.
4. Para registrar salidas, configurar Checklist:ResponsibleUserIds:Technology, HumanResources y Finance con IDs de usuarios reales existentes. Debe existir el empleado a dar de salida. No se crean empleados o vacantes ficticios al iniciar.
5. Iniciar con `dotnet run --project src/RelayHR.Web --launch-profile https` y abrir la URL HTTPS anunciada. Si el perfil tiene otro nombre, usar el configurado en launchSettings.json. Iniciar sesión desde la pantalla principal.
6. Registrar una salida para un empleado válido. El worker revisa solicitudes cada 10 segundos por defecto. Entrar en Vacantes Express; abrir la vacante PendingReview generada por HU-04.
7. Guardar cambios incompletos: se muestran pendientes. Intentar publicar: se rechaza con los campos faltantes. Completar los cuatro campos, guardar y confirmar publicación.
8. Verificar Published, fecha UTC, usuario y métrica. Intentar editar o republicar: la UI queda bloqueada y la API responde 409.
9. Abrir `/vacancies/{id}` o `/api/public/vacancies/{id}` sin sesión: solo se muestran datos para candidatos. Una pendiente o ID inexistente devuelve 404.
10. Opcionalmente configurar Jwt:Issuer, Audience y Key si se utiliza una autoridad externa de tokens. El login web sigue emitiendo cookies y no emite JWT.

No se creó commit ni se publicó la rama.
