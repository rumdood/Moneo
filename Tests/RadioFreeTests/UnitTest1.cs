using System.Diagnostics;
using AutoFixture;
using MediatR;
using Moq;
using RadioFreeBot.Features.AddSongToPlaylist;
using RadioFreeBot;
using Shouldly;
using Moneo.Chat.Commands;
using Moneo.Common;

namespace RadioFreeTests;

public class AddSongToPlaylistWorkflowManagerTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IYouTubeMusicProxyClient> _client = new();
    private readonly AddSongByQueryWorkflowManager _manager;

    public AddSongToPlaylistWorkflowManagerTests()
    {
        _manager = new AddSongByQueryWorkflowManager(_mediator.Object, _client.Object);
    }

    [Fact]
    public async Task StartWorkflowAsync_ShouldStartWorkflowAndPromptForSong()
    {
        var chatId = _fixture.Create<long>();
        var playlistId = _fixture.Create<string>();
        var result = await _manager.StartWorkflowAsync(chatId, playlistId, null);
        result.ShouldNotBeNull();
        Debug.Assert(result.UserMessageText != null, "result.UserMessageText != null");
        result.UserMessageText.ShouldContain("What song do you want to add");
        result.Type.ShouldBe(ResultType.NeedMoreInfo);
    }

    [Fact]
    public async Task StartWorkflowAsync_ShouldReturnErrorIfWorkflowAlreadyExists()
    {
        var chatId = _fixture.Create<long>();
        var playlistId = _fixture.Create<string>();
        await _manager.StartWorkflowAsync(chatId, playlistId, null);
        var result = await _manager.StartWorkflowAsync(chatId, playlistId, null);
        result.Type.ShouldBe(ResultType.Error);
        Debug.Assert(result.UserMessageText != null, "result.UserMessageText != null");
        result.UserMessageText.ShouldContain("already in a workflow");
    }

    [Fact]
    public async Task ContinueWorkflowAsync_ShouldReturnErrorIfNoWorkflow()
    {
        var chatId = _fixture.Create<long>();
        var result = await _manager.ContinueWorkflowAsync(chatId, "test");
        result.Type.ShouldBe(ResultType.Error);
        Debug.Assert(result.UserMessageText != null, "result.UserMessageText != null");
        result.UserMessageText.ShouldContain("don't seem to be doing anything");
    }

    [Fact]
    public async Task ContinueWorkflowAsync_ShouldHandleSongNotFound()
    {
        var chatId = _fixture.Create<long>();
        var playlistId = _fixture.Create<string>();
        _client.Setup(c => c.FindSongAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneoResult<List<RadioFreeBot.Models.SongItem>>.Failed("Song not found"));
        await _manager.StartWorkflowAsync(chatId, playlistId, "song");
        var result = await _manager.ContinueWorkflowAsync(chatId, "song");
        result.Type.ShouldBe(ResultType.Error);
        Debug.Assert(result.UserMessageText != null, "result.UserMessageText != null");
        result.UserMessageText.ShouldContain("couldn't find that song");
    }

    [Fact]
    public async Task ContinueWorkflowAsync_ShouldHandleSongFoundAndSelection()
    {
        var chatId = _fixture.Create<long>();
        var playlistId = _fixture.Create<string>();
        var song = _fixture.Build<RadioFreeBot.Models.SongItem>()
            .With(x => x.Id, "1")
            .With(x => x.Title, "Test Song")
            .With(x => x.Artist, "Test Artist")
            .With(x => x.Album, "Test Album")
            .Create();
        _client.Setup(c => c.FindSongAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneoResult<List<RadioFreeBot.Models.SongItem>>.Failed("Song not found"));
        await _manager.StartWorkflowAsync(chatId, playlistId, "song");
        var result = await _manager.ContinueWorkflowAsync(chatId, "song");
        result.Type.ShouldBe(ResultType.NeedMoreInfo);
        Debug.Assert(result.UserMessageText != null, "result.UserMessageText != null");
        result.UserMessageText.ShouldContain("Which one do you want to add");
    }
}

