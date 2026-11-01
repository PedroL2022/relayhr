using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.DTOs;
using RelayHR.Web.Repository;

namespace RelayHR.Web.Controllers
{
    [ApiController]
    [Route("api/freelancers")]
    public class FreelancersController : ControllerBase
    {
        private readonly IFreelancerRepository _repository;

        // Inyección de dependencias: el controlador pide el repositorio
        public FreelancersController(IFreelancerRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] FreelancerSearchFilterDTO filter)
        {
            // 1. Validación de parámetros de entrada
            if (filter.Page <= 0 || filter.PageSize <= 0 || filter.PageSize > 100)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Los parámetros de paginación (page y pageSize) deben ser mayores a cero y pageSize no puede exceder 100."
                });
            }

            if (filter.MinRate < 0 || filter.MaxRate < 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Las tarifas no pueden ser negativas."
                });
            }

            if (filter.MinRate.HasValue && filter.MaxRate.HasValue && filter.MinRate > filter.MaxRate)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "La tarifa mínima (minRate) no puede ser mayor que la tarifa máxima (maxRate)."
                });
            }

            if (filter.AvailableFrom.HasValue && filter.AvailableUntil.HasValue && filter.AvailableFrom > filter.AvailableUntil)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "La fecha inicial (availableFrom) no puede ser posterior a la fecha final (availableUntil)."
                });
            }

            // 2. Obtener la consulta base desde el Repositorio
            var query = _repository.GetActiveProfilesQuery();

            // 3. Aplicar filtros a la 'query'
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.MinRate.HasValue)
            {
                query = query.Where(p => p.HourlyRate >= filter.MinRate.Value);
            }

            if (filter.MaxRate.HasValue)
            {
                query = query.Where(p => p.HourlyRate <= filter.MaxRate.Value);
            }

            if (filter.AvailableFrom.HasValue && filter.AvailableUntil.HasValue)
            {
                var from = filter.AvailableFrom.Value;
                var until = filter.AvailableUntil.Value;
                query = query.Where(p => p.Availabilities.Any(a => a.AvailableFrom <= from && a.AvailableUntil >= until));
            }
            else if (filter.AvailableFrom.HasValue)
            {
                var from = filter.AvailableFrom.Value;
                query = query.Where(p => p.Availabilities.Any(a => a.AvailableFrom <= from && a.AvailableUntil >= from));
            }
            else if (filter.AvailableUntil.HasValue)
            {
                var until = filter.AvailableUntil.Value;
                query = query.Where(p => p.Availabilities.Any(a => a.AvailableFrom <= until && a.AvailableUntil >= until));
            }

            // 4. Ejecutar consulta, paginar y retornar
            var totalResults = await query.CountAsync();

            if (totalResults == 0)
            {
                return Ok(new ApiResponse<FreelancerSearchResponseDataDTO>
                {
                    Success = true,
                    Message = "Sin resultados.",
                    Data = new FreelancerSearchResponseDataDTO
                    {
                        TotalResults = 0,
                        Page = filter.Page,
                        PageSize = filter.PageSize,
                        Freelancers = new List<FreelancerSearchItemDTO>()
                    }
                });
            }

            var freelancers = await query
                .OrderBy(p => p.HourlyRate)
                .ThenBy(p => p.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new FreelancerSearchItemDTO
                {
                    Id = p.Id,
                    ProfessionalTitle = p.ProfessionalTitle,
                    Category = p.Category.Name,
                    HourlyRate = p.HourlyRate,
                    Currency = p.Currency,
                    IsAvailable = p.Availabilities.Any()
                })
                .ToListAsync();

            return Ok(new ApiResponse<FreelancerSearchResponseDataDTO>
            {
                Success = true,
                Message = "Trabajadores encontrados correctamente.",
                Data = new FreelancerSearchResponseDataDTO
                {
                    TotalResults = totalResults,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    Freelancers = freelancers
                }
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Obtener el detalle desde el Repositorio
            var p = await _repository.GetProfileDetailsAsync(id);

            if (p == null) return NotFound(new ApiResponse<object> { Success = false, Message = "El perfil solicitado no existe o no está disponible para consulta." });

            // Mapear a DTO y retornar
            var dto = new FreelancerProfileResponseDTO
            {
                Id = p.Id,
                ProfessionalTitle = p.ProfessionalTitle,
                Category = p.Category.Name,
                Experience = p.Experience,
                HourlyRate = p.HourlyRate,
                Currency = p.Currency,
                Availability = p.Availabilities
                    .OrderBy(a => a.AvailableFrom)
                    .Select(a => new AvailabilityItemDTO
                    {
                        AvailableFrom = a.AvailableFrom,
                        AvailableUntil = a.AvailableUntil
                    }).ToList(),
                Portfolio = p.Portfolios
                    .Select(pf => new PortfolioItemDTO
                    {
                        Title = pf.Title,
                        Description = pf.Description,
                        ProjectUrl = pf.ProjectUrl
                    }).ToList()
            };

            return Ok(new ApiResponse<FreelancerProfileResponseDTO> { Success = true, Message = "Perfil obtenido correctamente.", Data = dto });
        }
    }
}