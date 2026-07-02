using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities.Bhyt;

namespace ProDiabHis.Infrastructure.Bhyt;

/// <summary>
/// Parse XML ket qua doi soat tu cong BHYT.
/// Format XML gia dinh:
///   &lt;KetQuaGiamDinh&gt;
///     &lt;Items&gt;
///       &lt;Item tableNo="1" maLienKet="..." requestAmount="..." approvedAmount="..." status="APPROVED|REJECTED|ADJUSTED" rejectionCode="..." rejectionReason="..."&gt;
///     &lt;/Items&gt;
///   &lt;/KetQuaGiamDinh&gt;
/// </summary>
public class BhytReconcileParserImpl : IBhytReconcileParser
{
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<BhytReconcileParserImpl> _logger;

    public BhytReconcileParserImpl(IFileStorage fileStorage, ILogger<BhytReconcileParserImpl> logger)
    {
        _fileStorage = fileStorage; _logger = logger;
    }

    public async Task<BhytReconcileParseResult> ParseAsync(string filePath, CancellationToken ct)
    {
        _logger.LogInformation("BhytReconcileParser: parsing {Path}", filePath);
        try
        {
            // Doc file tu MinIO
            await using var stream = await _fileStorage.DownloadAsync("prodiab-his", filePath, ct);
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);

            var items = new List<BhytReconcileItemData>();
            var root = doc.Root;

            if (root == null)
                return new BhytReconcileParseResult(false, [], "XML khong co root element");

            // Ho tro ca 2 format: <KetQuaGiamDinh><Items> hoac <Items> la root
            var itemElements = root.Descendants("Item").ToList();

            foreach (var el in itemElements)
            {
                var tableNo = int.TryParse(el.Attribute("tableNo")?.Value, out var tn) ? tn : 1;
                var maLienKet = el.Attribute("maLienKet")?.Value ?? el.Element("maLienKet")?.Value ?? "";
                var requestAmount = decimal.TryParse(el.Attribute("requestAmount")?.Value, out var ra) ? ra : 0m;
                var approvedAmount = decimal.TryParse(el.Attribute("approvedAmount")?.Value, out var aa) ? aa : 0m;
                var statusStr = el.Attribute("status")?.Value ?? "APPROVED";
                var rejectionCode = el.Attribute("rejectionCode")?.Value;
                var rejectionReason = el.Attribute("rejectionReason")?.Value;

                var rejected = requestAmount - approvedAmount;

                // Normalize status
                var status = statusStr.ToUpperInvariant() switch
                {
                    "APPROVED" or "DUYET" => BhytReconcileItemStatus.Approved,
                    "REJECTED" or "TU_CHOI" => BhytReconcileItemStatus.Rejected,
                    "ADJUSTED" or "DIEU_CHINH" => BhytReconcileItemStatus.Adjusted,
                    _ => BhytReconcileItemStatus.Approved
                };

                items.Add(new BhytReconcileItemData(
                    TableNo: tableNo,
                    MaLienKet: maLienKet,
                    RequestAmount: requestAmount,
                    ApprovedAmount: approvedAmount,
                    RejectedAmount: Math.Max(0, rejected),
                    Status: status,
                    RejectionCode: rejectionCode,
                    RejectionReason: rejectionReason));
            }

            _logger.LogInformation("BhytReconcileParser: parsed {Count} items", items.Count);
            return new BhytReconcileParseResult(true, items, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BhytReconcileParser: parse failed {Path}", filePath);
            return new BhytReconcileParseResult(false, [], ex.Message);
        }
    }
}
