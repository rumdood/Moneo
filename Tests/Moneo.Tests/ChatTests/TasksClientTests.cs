using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moneo.Common;
using Moq.Protected;
using Moneo.TaskManagement.Contracts.Models;
using Moneo.Hosts.Chat.Api.Tasks;
using Moneo.Web;

namespace Moneo.Tests.ChatTests;

/// <summary>
/// These tests mostly validate that the JSON serialization and deserialization is working correctly.
/// </summary>
public class TasksClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly TasksClient _tasksClient;
    private readonly IFixture _fixture;
    
    private static async Task<HttpResponseMessage> ConvertToHttpResponseMessage(IResult result)
    {
        var context = new DefaultHttpContext();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
        serviceCollection.AddSingleton<ILogger<IResult>, Logger<IResult>>();
        context.RequestServices = serviceCollection.BuildServiceProvider();

        // Create a memory stream to capture the response body
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await result.ExecuteAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var httpResponseMessage = new HttpResponseMessage((HttpStatusCode)context.Response.StatusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8)
        };
        httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        foreach (var header in context.Response.Headers)
        {
            httpResponseMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        return httpResponseMessage;
    }

    public TasksClientTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _httpMessageHandlerMock = _fixture.Freeze<Mock<HttpMessageHandler>>();
        
        var config = new TaskManagementConfig
        {
            BaseUrl = "http://localhost",
            ApiKey = "foo"
        };
        
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        httpClient.BaseAddress = new Uri(config.BaseUrl);
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
        
        _tasksClient = new TasksClient(httpClient, config);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldReturnSuccessResult()
    {
        var requestDto = new CreateEditTaskDto(
            _fixture.Create<string>(), 
            _fixture.Create<string>(),
            true,
            ["Completed"],
            true, 
            ["Skipped"],
            "Pacific",
            DateTimeOffset.Now,
            null,
            null
        );
        
        var responseDto = _fixture.Build<MoneoTaskDto>()
            .With(dto => dto.Name, requestDto.Name)
            .With(dto => dto.Description, requestDto.Description)
            .With(dto => dto.DueOn, requestDto.DueOn)
            .With(dto => dto.IsActive, requestDto.IsActive)
            .With(dto => dto.CompletedMessages, requestDto.CompletedMessages)
            .With(dto => dto.CanBeSkipped, requestDto.CanBeSkipped)
            .With(dto => dto.SkippedMessages, requestDto.SkippedMessages)
            .With(dto => dto.Timezone, requestDto.Timezone)
            .With(dto => dto.Repeater, requestDto.Repeater)
            .With(dto => dto.Badger, requestDto.Badger)
            .Create();

        var response = MoneoResult<MoneoTaskDto>.Created(responseDto).GetHttpResult();
        var httpResponseMessage = await ConvertToHttpResponseMessage(response);

        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _tasksClient.CreateTaskAsync(1, requestDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(requestDto.Name, result.Data.Name);
        Assert.Equal(requestDto.Description, result.Data.Description);
        Assert.Equal(requestDto.DueOn, result.Data.DueOn);
        Assert.NotNull(result.Data.Id);
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldReturnSuccessResult()
    {
        var requestDto = new CreateEditTaskDto(
            _fixture.Create<string>(), 
            _fixture.Create<string>(),
            true,
            ["Completed"],
            true, 
            ["Skipped"],
            "Pacific",
            DateTimeOffset.Now,
            null,
            null
        );
        
        var response = MoneoResult.Success().GetHttpResult();
        var httpResponseMessage = await ConvertToHttpResponseMessage(response);

        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _tasksClient.UpdateTaskAsync(1, requestDto);

        // Assert
        Assert.True(result.IsSuccess);
    }
}