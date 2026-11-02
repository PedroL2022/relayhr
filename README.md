# RelayHR

Plataforma web para organizar el relevo de personal cuando un colaborador clave se va:
checklist de salida, traspaso de conocimiento, **Vacantes Express** de reemplazo y directorio de
**talento interino** (trabajadores independientes).

Proyecto de Programación III (ITLA) planificado con Scrum.

**Stack:** ASP.NET Core 10 (MVC + Razor) · Entity Framework Core 10 · SQL Server · patrón Repository.

---

## Cómo correrlo

Requisitos: SDK de .NET 10 y SQL Server (LocalDB viene con Visual Studio en Windows).

### Opción A — la más simple (EF crea la base)

```bash
dotnet run
```

Al arrancar, la aplicación aplica la migración (`Database.Migrate()`) y, **solo en Development**,
carga datos de demostración si la base está vacía. Abrir la URL que muestre la consola.

### Opción B — con los scripts SQL de la carpeta `Database/`

1. Ejecutar `Database/RelayHR_01_Esquema.sql` (crea la base `RelayHR`, las tablas y marca la migración como aplicada).
2. Opcional: `Database/RelayHR_02_DatosDemo.sql` (datos de demostración).
3. `dotnet run`.

> Usar la A **o** la B, no las dos sobre la misma base. Para reiniciar: `DROP DATABASE RelayHR;`
> (o `dotnet ef database drop`).

La cadena de conexión está en `appsettings.json` (`(localdb)\MSSQLLocalDB` por defecto). En Linux/Mac
apúntala a un SQL Server en Docker.

### Cuentas de demostración

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador de RRHH | admin@relayhr.com | Admin123! |
| Reclutador | reclutador@relayhr.com | Reclutador123! |
| Trabajador independiente | freelancer@relayhr.com | Freelancer123! |

También se puede crear cuenta desde **Registrarse**.

---

## Arquitectura

```
Controllers  →  Services  →  Repository  →  AppDbContext (EF Core)  →  SQL Server
 (HTTP/vistas)  (reglas)     (consultas)
```

| Carpeta | Qué contiene |
|---|---|
| `Entities/` | Modelo del dominio. Las reglas simples viven en la entidad (por ejemplo `EmployeeExit.TryClose`, `ExpressVacancy.TryPublish`). |
| `Repository/` | **Patrón Repository.** `IRepository<T>` + un repositorio por agregado + `IUnitOfWork`. |
| `Services/` | Lógica de negocio: `ExitService`, `VacancyService`, `TalentService`. |
| `Controllers/` + `Views/` | Interfaz MVC. Los controladores **no** usan `DbContext`. |
| `Data/` | `AppDbContext`, configuraciones Fluent API, migraciones y datos de demostración. |
| `Database/` | Scripts SQL (esquema y datos demo). |
| `RelayHR.Tests/` | Pruebas unitarias (xUnit + EF InMemory). |

### Cómo funciona el Repository

- Cada repositorio tiene métodos con nombre claro (`GetInboxAsync`, `GetByAssigneeAsync`…); nadie fuera de `Repository/` escribe LINQ/EF.
- Los métodos de **solo lectura** usan `AsNoTracking`; los que devuelven algo para modificar lo dejan rastreado (`...ForUpdateAsync`).
- Los repositorios **no guardan**: agregan/modifican y el servicio llama a `IUnitOfWork.SaveChangesAsync()`. Por eso registrar una salida crea la salida, su checklist y su vacante en **una sola transacción**.
- Se registran en `Program.cs` (inyección de dependencias).

---

## Historias de usuario → código

| HU | Qué hace | Dónde |
|---|---|---|
| HU-01 | Registrar salida y generar checklist por departamento | `ExitsController.Create` → `ExitService.RegisterAsync`; plantilla en `ExitChecklistTemplate` |
| HU-02 | Seguimiento, devolución de equipos, revocación de accesos y cierre bloqueado | `ExitsController.Details/SetTaskStatus/Close` → `EmployeeExit.TryClose` |
| HU-03 | Traspaso de conocimiento y responsables temporales | `HandoverController` → `ExitService.AddKnowledgeItemAsync/AssignKnowledgeItemAsync` |
| HU-04 | Vacante Express automática | `ExpressVacancy.CreateFromExit` (se genera al registrar la salida) |
| HU-05 | Revisar, completar y publicar (meta < 48 h) | `VacanciesController` → `VacancyService`; `ExpressVacancy.TryPublish/MetPublishGoal` |
| HU-06 | Buscar y filtrar independientes | `DirectoryController` → `FreelancerRepository.ApplyFilters` |
| HU-07 | Solicitud de servicio con un clic y su estado | `ServiceRequestsController`/`FreelancerController` → `TalentService` |

Vacíos que detectó la revisión del documento y quedaron cubiertos: perfiles de independientes,
registro/login/roles (`Security/Roles.cs`), una página pública de ofertas para candidatos (`/Jobs`),
y tareas obligatorias marcadas en el checklist.

**Fuera de alcance** (igual que el documento): nómina, pagos, contratos, notificaciones, calificaciones y dashboard avanzado.

**Plazos del documento:** el checklist (10 s) y la vacante (5 min) se generan en la misma operación
que registra la salida, así que están disponibles al instante.

### Roles

| Rol | Puede |
|---|---|
| Administrador de RRHH | Todo el lado empresa: empleados, puestos, salidas, traspaso, vacantes, directorio y solicitudes |
| Reclutador | Bandeja de vacantes, directorio y solicitudes |
| Trabajador independiente | Su perfil y las solicitudes que recibe |
| Sin sesión | Ver las ofertas publicadas |

> Simplificación académica: en **Registrarse** cualquiera puede elegir cualquier rol. En producción
> la asignación de roles de empresa debería controlarla un administrador.

---

## Pruebas

```bash
dotnet test
```

Cubren las reglas del dominio, los servicios y los repositorios (EF InMemory, sin SQL Server).

---

## Cambiar el modelo (nuevas migraciones)

```bash
dotnet ef migrations add NombreDelCambio -o Data/Migrations
dotnet ef database update
```

Si al arrancar aparece *"The model for context 'AppDbContext' has pending changes"*, el modelo y el
snapshot no coinciden: borra la carpeta `Data/Migrations`, haz `dotnet ef database drop` y vuelve a
generar con `dotnet ef migrations add InitialCreate -o Data/Migrations`.
