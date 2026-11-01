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

        public IQueryable<FreelancerProfile> GetActiveProfilesQuery()
        {
            // Devuelve la consulta base sin ejecutarla todavía (para que el controlador aplique los filtros)
            return _context.FreelancerProfiles
                .Include(p => p.Category)
                .Include(p => p.Availabilities)
                .AsNoTracking()
                .Where(p => p.IsActive && p.Category.IsActive);
        }

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
    }
}