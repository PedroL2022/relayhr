using RelayHR.Web.DTOs.KnowledgeTransfer;

namespace RelayHR.Web.Services.KnowledgeTransfer;

public interface IKnowledgeTransferService
{
    Task<OperationResult<KnowledgeTransferProjectResponseDTO>> CreateProjectAsync(
        int employeeExitId, CreateKnowledgeTransferProjectDTO dto, CancellationToken cancellationToken = default);

    Task<OperationResult<KnowledgeTransferTaskResponseDTO>> CreateTaskAsync(
        int employeeExitId, CreateKnowledgeTransferTaskDTO dto, CancellationToken cancellationToken = default);

    Task<OperationResult<AssignmentResponseDTO>> AssignTemporaryResponsibleAsync(
        int taskId, int? callerUserId, AssignTemporaryResponsibleDTO dto, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyList<MyTaskDTO>>> GetMyTasksAsync(
        int? userId, CancellationToken cancellationToken = default);

    Task<OperationResult<KnowledgeTransferOverviewDTO>> GetOverviewAsync(
        int employeeExitId, CancellationToken cancellationToken = default);
}
