using System.Diagnostics;

namespace ApduWebMonitor.External;

public class UsbReader(ILogger<UsbReader> logger)
{
    public async Task RestartSmartCardReaders()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pnputil.exe",
            Arguments = "/restart-device /class \"SmartCardReader\"",
            Verb = "runas", // requires admin
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            logger.LogError("Could not start PnPUtil process");
            return;
        }

        await process.WaitForExitAsync();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        logger.LogInformation("PnPUtil: {Message}", output);
        if (!string.IsNullOrWhiteSpace(error))
            logger.LogError("Error: {Error}", error);
    }
}
