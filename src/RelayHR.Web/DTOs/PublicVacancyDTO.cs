using RelayHR.Web.Entities;
namespace RelayHR.Web.Dtos;
public sealed record PublicVacancyDTO(int Id, string JobTitle, string Department,
    string JobDescription, string Requirements, DateTime PublishedAt)
{
    public static PublicVacancyDTO FromEntity(ExpressVacancy v) => new(v.ExpressVacancyId,
        v.JobTitle!, v.Department!.Name, v.JobDescription!, v.Requirements!,
        DateTime.SpecifyKind(v.PublishedAt!.Value, DateTimeKind.Utc));
}
