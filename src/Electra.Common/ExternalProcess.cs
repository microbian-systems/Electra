using System.Diagnostics;
using Electra.Common.Extensions;

namespace Electra.Common;

public static class ExternalProcess
{
    public static void ExecuteCommand(string command, string arguments, ILogger log)
    {
        //var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
        var processInfo = new ProcessStartInfo(command, arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        try
        {
            var process = Process.Start(processInfo);

            if (process is null)
            {
                log.LogError("unable to create process: {0}", command.ToJson());
                return;
            }
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                log.LogInformation("output>>" + e.Data);
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                log.LogError("error>>" + e.Data);
            process.BeginErrorReadLine();

            process.WaitForExit();

            log.LogInformation("ExitCode: {0}", process.ExitCode);
            process.Close();
            log.LogInformation($"process closed");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "error occurred while executing command: {0}",
                command.ToJson());
        }
    }
}