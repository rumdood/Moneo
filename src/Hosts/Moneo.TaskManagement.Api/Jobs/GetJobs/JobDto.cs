using Quartz;

namespace Moneo.TaskManagement.Jobs.GetJobs;

public record JobDto(
    string KeyName, 
    string GroupName, 
    string? JobType = null, 
    Dictionary<string, object?>? JobDataMap = null, 
    string? Description = null);