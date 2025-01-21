using Moneo.TaskManagement.Contracts.Models;

namespace Moneo.TaskManagement.ResourceAccess.Entities;

public record TaskBadger(int BadgerFrequencyInMinutes, List<string> BadgerMessages)
{
    public static TaskBadger FromDto(TaskBadgerDto dto)
    {
        return new TaskBadger(dto.BadgerFrequencyInMinutes, dto.BadgerMessages.ToList());
    }
    
    public TaskBadgerDto ToDto()
    {
        return new TaskBadgerDto(BadgerFrequencyInMinutes, BadgerMessages);
    }
}