namespace RelayHR.Web.Models.DTOs.ServiceRequests;

// Envoltorio { success, message, data } del PDF. Si tu equipo ya tiene uno común,
// reemplaza este tipo por el compartido.
public class ServiceRequestApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}