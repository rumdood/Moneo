using AutoFixture.AutoMoq;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.ResourceAccess.Entities;

namespace Moneo.Tests.TaskManagementApiTests;

public abstract class TaskManagementApiTestBase : IAsyncLifetime
{
    protected MoneoTasksDbContext DbContext;
    protected readonly IFixture Fixture;
    protected Mock<TimeProvider> TimeProvider;
    protected Mock<IMediator> Mediator;

    protected TaskManagementApiTestBase()
    {
        Fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    protected MoneoTasksDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MoneoTasksDbContext>()
            .UseInMemoryDatabase(databaseName: "MoneoTasksTestDb")
            .Options;
        return new MoneoTasksDbContext(options, TimeProvider.Object, Mediator.Object);
    }
    protected async Task ResetDatabase()
    {
        DbContext.Tasks.RemoveRange(DbContext.Tasks);
        DbContext.Conversations.RemoveRange(DbContext.Conversations);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
    }

    public Task InitializeAsync()
    {
        Mediator = Fixture.Freeze<Mock<IMediator>>();
        TimeProvider = Fixture.Freeze<Mock<TimeProvider>>();

        TimeProvider.Setup(t => t.GetUtcNow())
            .Returns(new DateTime(2025, 1, 10, 10, 0, 0));

        var options = new DbContextOptionsBuilder<MoneoTasksDbContext>()
            .UseInMemoryDatabase(databaseName: Fixture.Create<string>())
            .Options;

        DbContext = new MoneoTasksDbContext(options, TimeProvider.Object, Mediator.Object);
        Fixture.Inject(DbContext);

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => ResetDatabase();
}

internal static class FixtureExtensions
{
    private static long _id = 0;
    
    private static long GetNextId() => Interlocked.Increment(ref _id);
    
    public static IEnumerable<Conversation> CreateConversations(this IFixture fixture, int count = 1)
    {
        var dbContext = fixture.Create<MoneoTasksDbContext>();
        var conversations = new List<Conversation>();
        for (var i = 0; i < count; i++)
        {
            var conversation = new Conversation(GetNextId(), Transport.Telegram);
            dbContext.Conversations.Add(conversation);
            conversations.Add(conversation);
        }
        
        dbContext.SaveChanges();

        return conversations;
    }
    
    public static IEnumerable<MoneoTask> CreateTasks(
        this IFixture fixture,
        TimeProvider timeProvider,
        int count = 1, 
        long? conversationId = null,
        bool active = true,
        string? name = null,
        string? description = null,
        string timezone = "Pacific",
        TaskRepeater? repeater = null,
        TaskBadger? badger = null,
        DateTime? dueDate = null)
    {
        var dbContext = fixture.Create<MoneoTasksDbContext>();
        var conversation = dbContext.Conversations.SingleOrDefault(c => c.Id == conversationId);
        
        var actualDueDate = dueDate is null && repeater is null
            ? timeProvider.GetUtcNow().AddDays(10).UtcDateTime
            : dueDate;
        
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        name ??= fixture.Create<string>();
        description ??= fixture.Create<string>();

        var tasks = fixture.Build<MoneoTask>()
            .FromFactory(() => new MoneoTask(name, timezone, conversation))
            .With(t => t.IsActive, active)
            .With(t => t.Description, description)
            .With(t => t.Repeater, repeater)
            .With(t => t.Badger, badger)
            .With(t => t.DueOn, actualDueDate)
            .With(t => t.ConversationId, conversation.Id)
            .Without(t => t.DomainEvents)
            .Without(t => t.Name)
            .Without(t => t.Timezone)
            .CreateMany(count)
            .ToList();
    
        dbContext.Tasks.AddRange(tasks);
        dbContext.SaveChanges();
    
        return tasks;
    }
}
