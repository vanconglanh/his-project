using System.Diagnostics;
using Xunit;

namespace ProDiabHis.IntegrationTests;

/// <summary>
/// Fact chi chay khi Docker daemon dang san sang tren may/host chay test.
/// Neu khong co Docker (hoac daemon khong phan hoi) thi tu dong Skip voi ly do that,
/// khong hard-code Skip nhu truoc.
/// </summary>
public sealed class DockerAvailableFactAttribute : FactAttribute
{
    private static readonly Lazy<string?> SkipReason = new(DetectDockerUnavailableReason);

    public DockerAvailableFactAttribute()
    {
        Skip = SkipReason.Value;
    }

    private static string? DetectDockerUnavailableReason()
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
            var exited = process.WaitForExit(5000);
            if (!exited)
            {
                try { process.Kill(true); } catch { /* ignore */ }
                return "Khong the ket noi Docker daemon (timeout) — bo qua integration test yeu cau Docker.";
            }

            if (process.ExitCode != 0)
            {
                var stderr = process.StandardError.ReadToEnd();
                return $"Docker daemon khong san sang: {stderr.Trim()}";
            }

            return null; // Docker san sang -> khong skip
        }
        catch (Exception ex)
        {
            return $"Khong tim thay Docker CLI hoac daemon khong chay: {ex.Message}";
        }
    }
}
