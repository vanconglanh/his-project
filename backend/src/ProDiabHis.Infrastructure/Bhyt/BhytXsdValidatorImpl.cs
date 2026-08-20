using System.Data;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Bhyt;

/// <summary>
/// Validate XML file cua export voi XSD QD 3176 - validate THAT bang XmlSchemaSet + XDocument.Validate
/// (khong con la placeholder chi log "OK").
///
/// LUU Y: cac file bang{N}.xsd trong repo la PLACEHOLDER tu BYT chua co (xem ghi chu trong chinh
/// cac file .xsd va trong BhytXmlSerializerImpl) - dinh nghia long leo <Row><xs:any lax/></Row>.
/// Validate o day la THAT (khong gia lap), nhung chi kiem tra dung cau truc placeholder hien co
/// trong repo, KHONG the dam bao khop 100% XSD chinh thuc cua Bo Y te vi repo khong co ban that.
/// </summary>
public class BhytXsdValidatorImpl : IBhytXsdValidator
{
    private readonly ILogger<BhytXsdValidatorImpl> _logger;
    private readonly IDapperConnectionFactory _db;
    private readonly IFileStorage _storage;
    private readonly string _xsdBasePath;

    public BhytXsdValidatorImpl(ILogger<BhytXsdValidatorImpl> logger, IDapperConnectionFactory db, IFileStorage storage)
    {
        _logger = logger;
        _db = db;
        _storage = storage;
        _xsdBasePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Xsd", "qd3176");
    }

    public async Task<BhytXsdValidationResult> ValidateAsync(int exportId, CancellationToken ct)
    {
        _logger.LogInformation("BhytXsdValidator: validating exportId={Id}", exportId);

        var errors = new List<BhytValidationError>();

        using var conn = (IDbConnection)_db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT xml_file_path FROM diab_his_int_bhyt_exports WHERE id=@id", new { id = exportId });

        string? xmlFilePath = row?.xml_file_path;
        if (string.IsNullOrWhiteSpace(xmlFilePath))
        {
            errors.Add(new BhytValidationError(0, 0, "xml_file_path",
                "Chua sinh file XML (chay Generate XML truoc khi Validate)"));
            return new BhytXsdValidationResult(false, errors);
        }

        string xmlContent;
        try
        {
            await using var stream = await _storage.DownloadAsync(FileBuckets.BhytExports, xmlFilePath, ct);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            xmlContent = await reader.ReadToEndAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BhytXsdValidator: khong doc duoc file XML exportId={Id} path={Path}", exportId, xmlFilePath);
            errors.Add(new BhytValidationError(0, 0, "xml_file_path", $"Khong doc duoc file XML: {ex.Message}"));
            return new BhytXsdValidationResult(false, errors);
        }

        var result = ValidateXmlContent(xmlContent);
        _logger.LogInformation("BhytXsdValidator: exportId={Id} valid={Valid} errors={Count}",
            exportId, result.Valid, result.Errors.Count);
        return result;
    }

    /// <summary>
    /// Validate THAT noi dung XML voi XmlSchemaSet cho tung Bang1..Bang5 (tach rieng khoi I/O de
    /// unit test khong can DB/storage that).
    /// </summary>
    public BhytXsdValidationResult ValidateXmlContent(string xmlContent)
    {
        var errors = new List<BhytValidationError>();

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xmlContent, LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            errors.Add(new BhytValidationError(0, 0, "xml", $"File XML khong well-formed: {ex.Message} (dong {ex.LineNumber})"));
            return new BhytXsdValidationResult(false, errors);
        }

        for (int tableNo = 1; tableNo <= 5; tableNo++)
        {
            var xsdPath = Path.Combine(_xsdBasePath, $"bang{tableNo}.xsd");
            if (!File.Exists(xsdPath))
            {
                _logger.LogWarning("BhytXsdValidator: khong tim thay bang{N}.xsd, bo qua validate bang nay", tableNo);
                continue;
            }

            var bangEl = doc.Root?.Element($"Bang{tableNo}");
            if (bangEl is null)
            {
                errors.Add(new BhytValidationError(tableNo, 0, $"Bang{tableNo}",
                    $"Khong tim thay phan tu Bang{tableNo} trong file XML"));
                continue;
            }

            var schemas = new XmlSchemaSet();
            using (var xsdReader = XmlReader.Create(xsdPath))
            {
                schemas.Add(null, xsdReader);
            }

            // Validate doc lap tung Bang N (moi bang la 1 document goc rieng theo dung XSD placeholder)
            var standalone = new XDocument(new XElement(bangEl));
            standalone.Validate(schemas, (_, e) =>
            {
                var lineInfo = e.Exception is XmlSchemaException xse ? $" (dong {xse.LineNumber})" : "";
                errors.Add(new BhytValidationError(tableNo, 0, $"Bang{tableNo}",
                    $"{(e.Severity == XmlSeverityType.Error ? "Loi" : "Canh bao")} XSD: {e.Message}{lineInfo}"));
            });
        }

        return new BhytXsdValidationResult(errors.Count == 0, errors);
    }
}
