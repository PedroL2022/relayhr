namespace RelayHR.Web.Services;

/// <summary>
/// Error que no se arregla reintentando (por ejemplo, la salida ya no existe).
/// La solicitud pasa directamente a Failed en vez de reintentarse.
/// </summary>
public class ExpressVacancyPermanentException : Exception
{
    public ExpressVacancyPermanentException(string message) : base(message)
    {
    }
}
