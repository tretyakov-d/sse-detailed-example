using System.Text.Json;
using SseDetailedExample;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// used to generate occasional errors
var random = new Random();

app.MapGet("/sse", async ctx =>
{
    // local function writes input SsePayload to response
    Task SendEvent(SsePayload e) => ctx.Response.WriteAsync(e.ToString(), ctx.RequestAborted);

    var hosting = ctx.RequestServices.GetRequiredService<IHostApplicationLifetime>();
    using var requestOrServerAbort = CancellationTokenSource.CreateLinkedTokenSource(
        ctx.RequestAborted,
        hosting.ApplicationStopping);

    ctx.Response.Headers.Append("Content-Type", "text/event-stream; charset=utf-8");
    ctx.Response.Headers.Append("Cache-Control", "no-cache");
    
    var eventCounter = 0;
    
    if (ctx.Request.Headers.TryGetValue("Last-Event-ID", out var lastEventId))
        eventCounter = int.Parse(lastEventId.ToString());

    if (eventCounter == 0)
    {
        await SendEvent(new SsePayload
        {
            RetryInterval = 5000,
            Data = "Connection established\n" +
                   "Retry interval is set to 5 seconds\n"
        });
    }
    else
    {
        await SendEvent(new SsePayload
        {
            Data = "ReConnection successful\n" +
                   $"Serving messages since #{eventCounter}\n"
        });
    }

    while (await SleepUntilNextMessage(requestOrServerAbort.Token))
    {
        eventCounter++;

        if (eventCounter % 5 != 0)
        {
            await SendEvent(new SsePayload
            {
                Id = eventCounter.ToString(),
                Data = $"#{eventCounter} Server time: {DateTime.Now}"
            });   
        }
        else
        {
            await SendEvent(new SsePayload
            {
                Id = eventCounter.ToString(),
                EventName = "CUSTOM_EVENT",
                Data = JsonSerializer.Serialize(new
                {
                    CustomEventType = "Hello",
                    Message = $"Hello, this is custom event #{eventCounter}"
                })
            });
        }

        if (random.Next(10) == 0)
            throw new Exception("Unexpected error, to abort connection");
    }
});

async Task<bool> SleepUntilNextMessage(CancellationToken token)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(1), token);
        return true;
    }
    catch (TaskCanceledException)
    {
        return false;
    }
}

app.Run();