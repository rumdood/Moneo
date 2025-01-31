using MediatR;
using Microsoft.EntityFrameworkCore;
using Moneo.TaskManagement.DomainEvents;
using Moneo.TaskManagement.ResourceAccess;
using Moneo.TaskManagement.ResourceAccess.Entities;
using Moq;

namespace TaskManagementTests;

internal record TaskEntry(string Name, string Description);

internal record TaskEntryResult(Conversation Conversation, IReadOnlyList<MoneoTask> Tasks);

internal class TestFixture
{
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<TimeProvider> _timeProvider;
    private readonly Stack<DomainEvent> _domainEvents = new();
    
    public MoneoTasksDbContext DbContext { get; private set; }
    public TimeProvider TimeProvider => _timeProvider.Object;
    public IMediator Mediator => _mediator.Object;
    public Stack<DomainEvent> DomainEvents => _domainEvents;

    public IEnumerable<long> GetConversationIds()
        => DbContext.Conversations.AsNoTracking().Select(c => c.Id);
    
    public IEnumerable<long> GetTaskIds()
        => DbContext.Tasks.AsNoTracking().Select(t => t.Id);
    
    public TaskEntryResult InitConversationWithTasks(TaskEntry[] taskEntries)
    {
        var conversation = new Conversation(Transport.Telegram);
        var tasks = taskEntries
            .Select(entry => new MoneoTask(entry.Name, "Pacific", conversation)
            {
                Description = entry.Description,
                CompletedMessages = ["Completed message"],
                SkippedMessages = ["Skipped message"],
                DueOn = TimeProvider.GetUtcNow().DateTime,
                IsActive = true
            })
            .ToList();

        DbContext.Conversations.Add(conversation);
        DbContext.Tasks.AddRange(tasks);
        DbContext.SaveChanges();

        return new TaskEntryResult(conversation, tasks);
    }
    
    public void SetUtcNow(DateTime dateTime)
    {
        _timeProvider.Setup(x => x.GetUtcNow())
            .Returns(dateTime);
    }
    
    public void ResetDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void ResetDbContext()
    {
        var options = new DbContextOptionsBuilder<MoneoTasksDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        
        DbContext = new MoneoTasksDbContext(options, _timeProvider.Object, _mediator.Object);
    }

    public TestFixture()
    {
        _mediator = new Mock<IMediator>();
        _timeProvider = new Mock<TimeProvider>();
        _timeProvider.Setup(x => x.GetUtcNow())
            .Returns(new DateTime(2025, 01, 01, 0, 0, 0));

        _mediator.Setup(x => x.Publish(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback((DomainEvent de, CancellationToken _) =>
            {
                _domainEvents.Push(de);
            });
        
        ResetDbContext();
    }
}