using MediatR;

namespace Moneo.TelegramChat.Api.Features.GetStatus;

public static class GetStatusEndpoint
{
    public static void AddGetStatusEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/status", async (ISender sender) =>
        {
            var result = await sender.Send(new GetStatusRequest());
            return result.IsSuccess
                ? Results.Ok(result.Data!)
                : Results.Problem(title: "Internal Server Error", detail: result.Message);
        });
    }
}