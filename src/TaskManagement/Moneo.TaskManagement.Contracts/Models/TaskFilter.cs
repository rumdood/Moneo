using System.Diagnostics.CodeAnalysis;

namespace Moneo.TaskManagement.Contracts.Models;

public sealed record TaskFilter : IParsable<TaskFilter>
{
    public long? TaskId { get; set; }
    public IEnumerable<long>? TaskIds { get; set; }
    public string? Name { get; set; }
    public long? UserId { get; set; }
    public long? ConversationId { get; set; }
    public bool? IsActive { get; set; }
    public bool? CanBeSkipped { get; set; }
    
    public static TaskFilter Empty => new TaskFilter();
    public static TaskFilter ForTask(long taskId) => new TaskFilter { TaskId = taskId };
    public static TaskFilter ForTasks(IEnumerable<long> taskIds) => new TaskFilter { TaskIds = taskIds };
    public static TaskFilter ForUser(long userId) => new TaskFilter { UserId = userId };
    public static TaskFilter ForConversation(long conversationId) => new TaskFilter { ConversationId = conversationId };
    
    public TaskFilter WithTask(long taskId) => this with { TaskId = taskId };
    public TaskFilter WithTasks(IEnumerable<long> taskIds) => this with { TaskIds = taskIds };
    public TaskFilter WithName(string name) => this with { Name = name };
    public TaskFilter WithUser(long userId) => this with { UserId = userId };
    public TaskFilter WithConversationId(long conversationId) => this with { ConversationId = conversationId };
    public TaskFilter WithActive(bool isActive) => this with { IsActive = isActive };
    public TaskFilter WithSkippable(bool isSkippable) => this with { CanBeSkipped = isSkippable };

    /*
    public string ToSqlWhereConditions()
    {
        // generate a SQL query based on the filter
        var sqlBuilder = new StringBuilder();
        var conditions = new List<string>();
        
        if (TaskId.HasValue)
        {
            conditions.Add($"[id] = {TaskId.Value} ");
        }
        
        if (TaskId.HasValue == false && TaskIds != null && TaskIds.Any())
        {
            conditions.Add($"[id] in ({string.Join(',', TaskIds)}) ");
        }
        
        if (!string.IsNullOrEmpty(Name))
        {
            conditions.Add($"[name] like '%{Name}%' ");
        }
        
        if (UserId.HasValue)
        {
            conditions.Add($"[conversation_id] in (select [conversation_id] from [user_conversations] where [user_id] = {UserId.Value}) ");
        }
        
        if (ConversationId.HasValue)
        {
            conditions.Add($"[conversation_id] = {ConversationId.Value} ");
        }
        
        if (IsActive.HasValue)
        {
            conditions.Add($"[isActive] = {(IsActive.Value ? "1" : "0" )} ");
        }
        
        if (CanBeSkipped.HasValue)
        {
            conditions.Add($"[canBeSkipped] = {(CanBeSkipped.Value ? "1" : "0" )} ");
        }
        
        sqlBuilder.Append(string.Join(" and ", conditions));
        
        return sqlBuilder.ToString();
    }
    */

    public static TaskFilter Parse(string s, IFormatProvider? provider)
    {
        // this will only be used to parse query string parameters, parse the query string parameters here
        var query = System.Web.HttpUtility.ParseQueryString(s);
        var result = new TaskFilter
        {
            TaskId = long.TryParse(query["TaskId"], out var taskId) ? taskId : (long?)null,
            TaskIds = query["TaskIds"]?.Split(',').Select(long.Parse),
            Name = query["Name"],
            UserId = long.TryParse(query["UserId"], out var userId) ? userId : (long?)null,
            ConversationId = long.TryParse(query["ConversationId"], out var conversationId) ? conversationId : (long?)null,
            IsActive = bool.TryParse(query["IsActive"], out var isActive) ? isActive : (bool?)null,
            CanBeSkipped = bool.TryParse(query["CanBeSkipped"], out var canBeSkipped) ? canBeSkipped : (bool?)null
        };
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TaskFilter result)
    {
        if (string.IsNullOrEmpty(s))
        {
            result = null;
            return false;
        }
        
        try
        {
            result = Parse(s, provider);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
