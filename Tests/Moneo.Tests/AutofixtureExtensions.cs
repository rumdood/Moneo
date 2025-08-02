using Moneo.Chat;
using Moneo.Chat.Models;

namespace Moneo.Tests;

public static class AutoFixtureExtensions
{
    public static ChatUser GetUser(this Fixture fixture, long? id = null, string? firstname = null, string? lastname = null,
        string? username = null)
    {
        return fixture.Build<ChatUser>()
            .With(x => x.Id, id ?? fixture.Create<long>())
            .With(x => x.FirstName, firstname ?? fixture.Create<string>())
            .With(x => x.LastName, lastname ?? fixture.Create<string>())
            .With(x => x.Username, username ?? fixture.Create<string>())
            .Create();
    }

    public static CommandContext GetCommandContext(this Fixture fixture, long? conversationId = null, ChatUser? user = null,
        ChatState? initialState = null, string? commandText = null)
    {
        return CommandContextFactory.BuildCommandContext(
            conversationId ?? fixture.Create<long>(),
            user ?? fixture.GetUser(),
            initialState ?? ChatState.Waiting,
            commandText ?? fixture.Create<string>()
        );
    }
}