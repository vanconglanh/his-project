using System.IO.Compression;

namespace ProDiabHis.Application.Common;

/// <summary>Mot entry hop le da giai nen tu ZIP: ten file (khong duong dan) + noi dung bytes.</summary>
public record ExtractedZipEntry(string Name, byte[] Bytes);

/// <summary>Gioi han an toan khi giai nen ZIP (chong zip bomb).</summary>
/// <param name="MaxFiles">So file toi da lay ra.</param>
/// <param name="MaxEntryBytes">Kich thuoc toi da moi file (uncompressed khai bao).</param>
/// <param name="MaxTotalBytes">Tong dung luong giai nen toi da (chong zip bomb).</param>
public record ZipExtractLimits(int MaxFiles, long MaxEntryBytes, long MaxTotalBytes);

/// <summary>
/// Giai nen ZIP AN TOAN dung chung — tach ra tu co che da chay production trong LegacyOcrBatchJob
/// (chan path traversal, chan zip bomb theo tong dung luong + so file + kich thuoc moi file, bo qua
/// thu muc va file ngoai whitelist). KHONG viet lai o moi noi can dung ZIP.
/// </summary>
public static class SafeZipExtractor
{
    /// <summary>
    /// Duyet cac entry trong <paramref name="zipStream"/>, chi giu file thoa: khong phai thu muc,
    /// khong path traversal (".." hoac duong dan tuyet doi), ten hop le theo <paramref name="isAllowedName"/>,
    /// kich thuoc trong nguong. Dung khi vuot tong dung luong hoac dat so file toi da.
    /// </summary>
    public static async Task<IReadOnlyList<ExtractedZipEntry>> ExtractAsync(
        Stream zipStream, Func<string, bool> isAllowedName, ZipExtractLimits limits, CancellationToken ct)
    {
        var result = new List<ExtractedZipEntry>();
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        long totalExtractedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(entry.Name)) continue;                                   // thu muc
            if (entry.FullName.Contains("..") || Path.IsPathRooted(entry.FullName)) continue; // path traversal
            if (!isAllowedName(entry.Name)) continue;                                          // ngoai whitelist
            if (entry.Length <= 0 || entry.Length > limits.MaxEntryBytes) continue;           // rong / qua lon

            totalExtractedBytes += entry.Length;
            if (totalExtractedBytes > limits.MaxTotalBytes) break;                             // chan zip bomb

            await using var entryStream = entry.Open();
            using var fileMemory = new MemoryStream();
            await entryStream.CopyToAsync(fileMemory, ct);
            result.Add(new ExtractedZipEntry(entry.Name, fileMemory.ToArray()));

            if (result.Count >= limits.MaxFiles) break;                                        // dat so file toi da
        }

        return result;
    }
}
