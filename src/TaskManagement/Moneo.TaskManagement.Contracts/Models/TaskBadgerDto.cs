namespace Moneo.TaskManagement.Contracts.Models;

public record TaskBadgerDto(
    int BadgerFrequencyInMinutes,
    IReadOnlyList<string> BadgerMessages);