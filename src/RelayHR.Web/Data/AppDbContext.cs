using Microsoft.EntityFrameworkCore;
namespace RelayHR.Web.Data;
// HU-04 conserva su contrato de contexto, compartiendo el modelo canónico de salidas/Identity.
// Las nuevas migraciones de este flujo pertenecen a RelayHrDbContext.
public class AppDbContext(DbContextOptions<AppDbContext> options) : RelayHrDbContext(options);
