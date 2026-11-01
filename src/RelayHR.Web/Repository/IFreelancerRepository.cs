using RelayHR.Web.Models;

namespace RelayHR.Web.Repository
{
    public interface IFreelancerRepository
    {
        // Para el endpoint de búsqueda (Search)
        // HU-06: Lectura y Búsqueda
        IQueryable<FreelancerProfile> GetActiveProfilesQuery();
        Task<FreelancerProfile?> GetProfileDetailsAsync(int id);
        
        // Para el endpoint de detalle (GetById)
        // HU-09: Operaciones CRUD (Crear, Actualizar, Eliminar)
        Task<FreelancerProfile?> GetProfileForTrackingAsync(int id);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task<bool> UserHasProfileAsync(int userId);
        Task<FreelancerProfile> AddProfileAsync(FreelancerProfile profile);
        Task UpdateProfileAsync(FreelancerProfile profile);
        Task DeleteProfileAsync(FreelancerProfile profile);
    }
}