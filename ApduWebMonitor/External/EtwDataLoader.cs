using ApduWebMonitor.Services;
using Microsoft.Diagnostics.Tracing;

namespace ApduWebMonitor.External;

public class EtwDataLoader(
    ApduStore store,
    IConfiguration configuration,
    ILogger<EtwDataLoader> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sessionName = configuration.GetValue<string>(Constants.EtwSessionNameField);
        var source = new ETWTraceEventSource(sessionName, TraceEventSourceType.Session);
        source.AllEvents += HandleEvent;

        stoppingToken.Register(() =>
        {
            logger.LogInformation("Stopping ETW event processing...");
            source.StopProcessing();
            source.Dispose();
        });

        logger.LogInformation("Starting ETW event processing...");
        return Task.Run(() =>
        {
            try
            {
                source.Process();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ETW Process ERROR");
            }
        }, stoppingToken);
    }

    private void HandleEvent(TraceEvent evnt)
    {
        try
        {
            HandleEventInternal(evnt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ETW event");
        }
    }

    private void HandleEventInternal(TraceEvent evnt)
    {
        if (evnt.Opcode != TraceEventOpcode.Info)
        {
            logger.LogInformation($"Not Opcode Info");
            return;
        }

        var data = string.Join("", evnt.EventData().SkipLast(1).Select(x => (char)x));
        var splitData = data.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (splitData is not ["APDU", _])
        {
            logger.LogInformation("{Data} is not an APDU", data);
            return;
        }

        if (splitData[1] == "80 14 05 00 00") // weird APDU command - no response
            return;

        if (splitData[1] == "Reset")
        {
            logger.LogInformation("Reset");
            store.Reset();
            return;
        }

        logger.LogInformation("Received {APDU}", splitData[1]);

        var apduBytes = splitData[1]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Convert.ToByte(x, 16))
            .ToArray();

        store.AddMessage(apduBytes);
    }
}

