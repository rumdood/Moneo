using Moneo.TaskManagement.Models;

namespace Moneo.TaskManagement.Client.Models;

public record MoneoTaskManagerDto(MoneoTaskState Task, long ChatId);