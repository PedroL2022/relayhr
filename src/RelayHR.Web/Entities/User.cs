namespace RelayHR.Web.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int FailedLoginAttempts { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();
}