using System.Linq.Expressions;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.TaskManagement;

public static class TaskFilterExtensions
{
    public static Expression<Func<MoneoTask, bool>> ToExpression(this TaskFilter filter)
        {
            var parameter = Expression.Parameter(typeof(MoneoTask), "t");
            Expression? predicate = Expression.Constant(true);

            if (filter.TaskId.HasValue)
            {
                var taskIdExpression = Expression.Equal(
                    Expression.Property(parameter, nameof(MoneoTask.Id)),
                    Expression.Constant(filter.TaskId.Value)
                );
                predicate = Expression.AndAlso(predicate, taskIdExpression);
            }

            if (filter.TaskId.HasValue == false && filter.TaskIds != null && filter.TaskIds.Any())
            {
                var taskIdsExpression = Expression.Call(
                    Expression.Constant(filter.TaskIds),
                    typeof(IEnumerable<long>).GetMethod("Contains", [typeof(long)])!,
                    Expression.Property(parameter, nameof(MoneoTask.Id))
                );
                predicate = Expression.AndAlso(predicate, taskIdsExpression);
            }

            if (!string.IsNullOrEmpty(filter.Name))
            {
                var nameExpression = Expression.Call(
                    Expression.Property(parameter, nameof(MoneoTask.Name)),
                    nameof(string.Contains),
                    Type.EmptyTypes,
                    Expression.Constant(filter.Name)
                );
                predicate = Expression.AndAlso(predicate, nameExpression);
            }

            if (filter.UserId.HasValue)
            {
                var userConversations = Expression.Property(
                    Expression.Property(parameter, nameof(MoneoTask.Conversation)),
                    nameof(Conversation.UserConversations)
                );
                var userConversationsExpression = Expression.Call(
                    Expression.Constant(userConversations),
                    typeof(IEnumerable<long>).GetMethod("Contains", [typeof(long)])!,
                    Expression.Property(parameter, nameof(MoneoTask.ConversationId))
                );
                predicate = Expression.AndAlso(predicate, userConversationsExpression);
            }

            if (filter.ConversationId.HasValue)
            {
                var conversationIdExpression = Expression.Equal(
                    Expression.Property(parameter, nameof(MoneoTask.ConversationId)),
                    Expression.Constant(filter.ConversationId.Value)
                );
                predicate = Expression.AndAlso(predicate, conversationIdExpression);
            }

            if (filter.IsActive.HasValue)
            {
                var isActiveExpression = Expression.Equal(
                    Expression.Property(parameter, nameof(MoneoTask.IsActive)),
                    Expression.Constant(filter.IsActive.Value)
                );
                predicate = Expression.AndAlso(predicate, isActiveExpression);
            }

            if (!filter.CanBeSkipped.HasValue)
            {
                return Expression.Lambda<Func<MoneoTask, bool>>(predicate, parameter);
            }
            
            var canBeSkippedExpression = Expression.Equal(
                Expression.Property(parameter, nameof(MoneoTask.CanBeSkipped)),
                Expression.Constant(filter.CanBeSkipped.Value)
            );
            predicate = Expression.AndAlso(predicate, canBeSkippedExpression);

            return Expression.Lambda<Func<MoneoTask, bool>>(predicate, parameter);
        }
}