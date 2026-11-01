# Resultados de pruebas funcionales y de rendimiento — HU-01

## Alcance

Las pruebas unitarias cubren la fecha efectiva, la existencia del empleado, las
salidas activas duplicadas y la generación del checklist con responsables,
departamentos, fechas límite y estado inicial. Las pruebas de integración usan
`WebApplicationFactory` y una base SQLite en memoria para ejercitar la API HTTP,
Identity, las transacciones y la persistencia sin depender de una instancia
externa de SQL Server.

## Ejecución

```powershell
dotnet test tests\RelayHR.Web.Tests\RelayHR.Web.Tests.csproj --logger "console;verbosity=detailed"
dotnet build RelayHR.slnx
```

Resultado de la última ejecución:

- **15 pruebas aprobadas; 0 fallidas; 0 omitidas.**
- **Build de la solución aprobado; 0 advertencias; 0 errores.**
- **Dependencias vulnerables conocidas:** ninguna detectada en el proyecto de
  pruebas.

## Casos verificados

| Caso | Resultado |
|---|---|
| Fecha efectiva pasada | Rechazada por el servicio antes de guardar. |
| Empleado inexistente | La API devuelve `404 Not Found`. |
| Empleado con salida activa | La API devuelve `409 Conflict`. |
| Registro válido | Devuelve `201 Created` y persiste una salida, un checklist y seis tareas. |
| Generación del checklist | Crea tareas de Tecnología, Recursos Humanos y Finanzas con responsables, plazos y estado `Pending`. |
| Consulta del checklist | Devuelve las tareas ordenadas por departamento y fecha límite, sin modificar los registros. |
| Salida inexistente | La consulta devuelve `404 Not Found`. |
| Acceso anónimo / usuario sin rol | Devuelve `401 Unauthorized` / `403 Forbidden`. |
| Falla durante la generación | Devuelve `503 Service Unavailable` y no persiste la salida ni el checklist. |
| Falla al guardar una tarea | Un trigger de prueba fuerza un error SQL; la transacción revierte salida, checklist y tareas. |
| Solicitud duplicada | Devuelve `409 Conflict` y conserva un solo conjunto de registros. |

## Medición de disponibilidad

La prueba mide desde antes del `POST /api/employee-exits` hasta que termina con
éxito el `GET /api/employee-exits/{id}/checklist`. En la última ejecución, el
resultado local fue **49 ms**, inferior al máximo de 10 segundos.

La medición se realizó en el entorno de pruebas con SQLite en memoria y la
aplicación ya inicializada. No es un benchmark de SQL Server ni garantiza la
latencia de producción, que también depende del servidor, la red y la carga.
La migración y la conexión real de SQL Server deben verificarse en el entorno
de despliegue.

## Proyecto de pruebas

Las pruebas están en `tests/RelayHR.Web.Tests/`. Para ejecutarlas desde el
repositorio, usa el comando `dotnet test` mostrado arriba.
