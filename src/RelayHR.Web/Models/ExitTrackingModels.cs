namespace RelayHR.Web.Models;

public sealed record PendingTaskDTO(int Id, string Title, string Department, string Status);

public sealed record CloseBlockedData(List<PendingTaskDTO> PendingTasks);

public sealed record CloseExitResponse(
    int EmployeeExitId,
    string Status,
    DateTime ClosedAt,
    int ClosedByUserId);

public sealed record ExitProgressResponse(
    int EmployeeExitId,
    int TotalTasks,
    int CompletedTasks,
    int PendingTasks,
    int InProgressTasks,
    double ProgressPercentage,
    int RequiredIncompleteTasks,
    bool CanClose);