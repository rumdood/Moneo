using System.Linq.Expressions;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement;

public sealed record TaskFilter
{
    public long? TaskId { get; set; }
    public IEnumerable<long>? TaskIds { get; set; }
    public string? Name { get; set; }
    // public long? UserId { get; set; }
    public long? ConversationId { get; set; }
    public bool? IsActive { get; set; }
    public bool? CanBeSkipped { get; set; }
    
    public static TaskFilter Empty => new TaskFilter();
    public static TaskFilter ForTask(long taskId) => new TaskFilter { TaskId = taskId };
    public static TaskFilter ForTasks(IEnumerable<long> taskIds) => new TaskFilter { TaskIds = taskIds };
    // public static TaskFilter ForUser(long userId) => new TaskFilter { UserId = userId };
    public static TaskFilter ForConversation(long conversationId) => new TaskFilter { ConversationId = conversationId };
    
    public TaskFilter WithTask(long taskId) => this with { TaskId = taskId };
    public TaskFilter WithTasks(IEnumerable<long> taskIds) => this with { TaskIds = taskIds };
    public TaskFilter WithName(string name) => this with { Name = name };
    // public TaskFilter WithUser(long userId) => this with { UserId = userId };
    public TaskFilter WithConversationId(long conversationId) => this with { ConversationId = conversationId };
    public TaskFilter WithActive(bool isActive) => this with { IsActive = isActive };
    public TaskFilter WithSkippable(bool isSkippable) => this with { CanBeSkipped = isSkippable };
    
    public Expression<Func<MoneoTask, bool>> ToExpression()
        {
            var parameter = Expression.Parameter(typeof(MoneoTask), "t");
            Expression? predicate = Expression.Constant(true);

            if (TaskId.HasValue)
            {
                var taskIdExpression = Expression.Equal(
                    Expression.Property(parameter, nameof(MoneoTask.Id)),
                    Expression.Constant(TaskId.Value)
                );
                predicate = Expression.AndAlso(predicate, taskIdExpression);
            }

            if (TaskIds != null && TaskIds.Any())
            {
                var taskIdsExpression = Expression.Call(
                    Expression.Constant(TaskIds),
                    typeof(IEnumerable<long>).GetMethod("Contains", new[] { typeof(long) })!,
                    Expression.Property(parameter, nameof(MoneoTask.Id))
                );
                predicate = Expression.AndAlso(predicate, taskIdsExpression);
            }

            if (!string.IsNullOrEmpty(Name))
            {
                var nameExpression = Expression.Equal(
                    Expression.Property(parameter, nameof(MoneoTask.Name)),
                    Expression.Constant(Name)
                );
                predicate = Expression.AndAlso(predicate, nameExpression);
            }

            if (ConversationId.HasValue)
            {
                var conversationIdExpression = Expression.Equal(
                    Expression.Property(parameter, nameof(MoneoTask.ConversationId)),
                    Expression.Constant(ConversationId.Value)
                );
                predicate = Expression.AndAlso(predicate, conversationIdExpression);
            }

            if (IsActive.HasValue)
            {
                var isActiveExpression = Expression.Equal(
                    Expression.Property(parameter, nameof(MoneoTask.IsActive)),
                    Expression.Constant(IsActive.Value)
                );
                predicate = Expression.AndAlso(predicate, isActiveExpression);
            }

            if (!CanBeSkipped.HasValue)
            {
                return Expression.Lambda<Func<MoneoTask, bool>>(predicate, parameter);
            }
            
            var canBeSkippedExpression = Expression.Equal(
                Expression.Property(parameter, nameof(MoneoTask.CanBeSkipped)),
                Expression.Constant(CanBeSkipped.Value)
            );
            predicate = Expression.AndAlso(predicate, canBeSkippedExpression);

            return Expression.Lambda<Func<MoneoTask, bool>>(predicate, parameter);
        }
}
