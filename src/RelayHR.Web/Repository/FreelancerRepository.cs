using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Models;

namespace RelayHR.Web.Repository
{
    public class FreelancerRepository : IFreelancerRepository
{
        private readonly ApplicationDbContext _context;

        public FreelancerRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // HU-06: Consulta base para el buscador
        public IQueryable<FreelancerProfile> GetActiveProfilesQuery()
        {
            // Devuelve la consulta base sin ejecutarla todavía (para que el controlador aplique los filtros)
            return _context.FreelancerProfiles
                .Include(p => p.Category)
                .Include(p => p.Availabilities)
                .AsNoTracking()
                .Where(p => p.IsActive && p.Category.IsActive);
        }

        // HU-06: Consulta de detalle (Solo lectura)
        public async Task<FreelancerProfile?> GetProfileDetailsAsync(int id)
        {
            // Busca un perfil específico con todas sus relaciones
            return await _context.FreelancerProfiles
                .Include(p => p.Category)
                .Include(p => p.Availabilities)
                .Include(p => p.Portfolios)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.Category.IsActive);
        }

        // HU-09: Obtener perfil con Tracking para poder editarlo o eliminarlo
        public async Task<FreelancerProfile?> GetProfileForTrackingAsync(int id)
        {
            return await _context.FreelancerProfiles
                .Include(p => p.Category)
                .Include(p => p.Availabilities)
                .Include(p => p.Portfolios)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        public async Task<bool> CategoryExistsAsync(int categoryId)
        {
            return await _context.FreelancerCategories.AnyAsync(c => c.Id == categoryId && c.IsActive);
        }

        public async Task<bool> UserHasProfileAsync(int userId)
        {
            return await _context.FreelancerProfiles.AnyAsync(p => p.UserId == userId);
        }

       // HU-09: CREAR (Create)
        public async Task<FreelancerProfile> AddProfileAsync(FreelancerProfile profile)
        {
            // Verificar si el usuario existe; si no existe, creamos uno nuevo y usamos el Id real que le asigne SQL Server
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == profile.UserId);
            if (existingUser == null)
            {
                var newUser = new User
                {
                    FullName = $"Profesional Independiente",
                    Email = $"freelancer_{Guid.NewGuid().ToString()[..6]}@relayhr.com",
                    PasswordHash = "DEMO_HASH"
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Asignamos el Id real autogenerado por SQL Server para que la llave foránea nunca falle
                profile.UserId = newUser.Id;
            }

            _context.FreelancerProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        // HU-09: ACTUALIZAR (Update)
        public async Task UpdateProfileAsync(FreelancerProfile profile)
        {
            _context.FreelancerProfiles.Update(profile);
            await _context.SaveChangesAsync();
        }

        // HU-09: ELIMINAR (Delete - Baja para salir de las búsquedas)
        public async Task DeleteProfileAsync(FreelancerProfile profile)
        {
            _context.FreelancerProfiles.Remove(profile);
            await _context.SaveChangesAsync();
        }
    }
}