using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

var apiAppUrl = builder.Configuration["ApiApp:Url"] ?? "https://m1.caservices.visiondsm.com/application/";

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/data", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromSeconds(15);

    try
    {
        using var response = await client.GetAsync(apiAppUrl, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Json(
                new
                {
                    ok = false,
                    statusCode = (int)response.StatusCode,
                    reasonPhrase = response.ReasonPhrase ?? "Unknown error",
                    data = responseText
                },
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Json(new
        {
            ok = true,
            statusCode = (int)response.StatusCode,
            data = responseText
        });
    }
    catch (HttpRequestException exception)
    {
        return Results.Json(
            new
            {
                ok = false,
                statusCode = (int)HttpStatusCode.BadGateway,
                reasonPhrase = "Request to API App failed",
                data = exception.Message
            },
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (TaskCanceledException)
    {
        return Results.Json(
            new
            {
                ok = false,
                statusCode = (int)HttpStatusCode.GatewayTimeout,
                reasonPhrase = "Request to API App timed out",
                data = "The request to API App did not complete in time."
            },
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
});

app.MapFallbackToFile("index.html");

app.Run();
