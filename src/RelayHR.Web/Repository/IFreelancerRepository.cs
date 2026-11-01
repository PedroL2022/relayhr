using RelayHR.Web.Models;

namespace RelayHR.Web.Repository
{
    public interface IFreelancerRepository
    {
        // Para el endpoint de búsqueda (Search)
        IQueryable<FreelancerProfile> GetActiveProfilesQuery();
        
        // Para el endpoint de detalle (GetById)
        Task<FreelancerProfile?> GetProfileDetailsAsync(int id);
    }
}