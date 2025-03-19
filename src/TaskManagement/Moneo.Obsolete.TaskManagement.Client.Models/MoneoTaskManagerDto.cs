using Moneo.Obsolete.TaskManagement.Models;

namespace Moneo.Obsolete.TaskManagement.Client.Models;

public record MoneoTaskManagerDto(MoneoTaskState Task, long ChatId);