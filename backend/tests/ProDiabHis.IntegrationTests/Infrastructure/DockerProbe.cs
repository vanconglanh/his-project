using System.Diagnostics;

namespace ProDiabHis.IntegrationTests.Infrastructure;

/// <summary>
/// Do 1 lan duy nhat cho ca test run: Docker daemon co san sang khong.
/// Dung chung boi ApiTestFixture (bo qua khoi dong container) va ApiFactAttribute (skip test).
/// </summary>
public static class DockerProbe
{
    private static readonly Lazy<string?> _reason = new(Detect);

    /// <summary>Ly do Docker khong dung duoc; null = Docker san sang.</summary>
    public static string? UnavailableReason => _reason.Value;

    public static bool IsAvailable => _reason.Value is null;

    private static string? Detect()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "version --format \"{{.Server.Version}}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            if (!process.WaitForExit(10000))
            {
                try { process.Kill(true); } catch { /* ignore */ }
                return "Khong ket noi duoc Docker daemon (timeout 10s).";
            }

            if (process.ExitCode != 0)
                return $"Docker daemon khong san sang: {process.StandardError.ReadToEnd().Trim()}";

            return null;
        }
        catch (Exception ex)
        {
            return $"Khong tim thay Docker CLI hoac daemon khong chay: {ex.Message}";
        }
    }
}
