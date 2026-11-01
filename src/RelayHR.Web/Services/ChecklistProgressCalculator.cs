using RelayHR.Web.Entities;

namespace RelayHR.Web.Services;

public sealed record ChecklistProgress(
    int Total,
    int Completed,
    int Pending,
    int InProgress,
    double Percentage,
    int RequiredIncomplete,
    bool CanClose);

public static class ChecklistProgressCalculator
{
    public static ChecklistProgress Calculate(IReadOnlyCollection<ChecklistTask> tasks)
    {
        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == ChecklistTaskStatus.Completed);
        var pending = tasks.Count(t => t.Status == ChecklistTaskStatus.Pending);
        var inProgress = tasks.Count(t => t.Status == ChecklistTaskStatus.InProgress);
        var requiredIncomplete = tasks.Count(t =>
            t.IsRequired && t.Status != ChecklistTaskStatus.Completed);

        var percentage = total == 0 ? 0 : Math.Round(completed * 100.0 / total, 2);
        var canClose = total > 0 && requiredIncomplete == 0;

        return new ChecklistProgress(
            total, completed, pending, inProgress, percentage, requiredIncomplete, canClose);
    }
}