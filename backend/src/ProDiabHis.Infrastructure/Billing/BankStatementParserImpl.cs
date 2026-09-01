using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Billing.BankReconciliation;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>
/// Parser file sao ke ngan hang (.xlsx qua ClosedXML, .csv qua text thuan).
/// Cot ky vong (header dong 1, du lieu tu dong 2):
///   A: transaction_date (dd/MM/yyyy hoac o Date Excel), B: amount, C: reference_no, D: description
/// </summary>
public class BankStatementParserImpl : IBankStatementParser
{
    private readonly ILogger<BankStatementParserImpl> _logger;

    public BankStatementParserImpl(ILogger<BankStatementParserImpl> logger)
    {
        _logger = logger;
    }

    public Task<List<BankStatementRawLine>> ParseAsync(Stream fileStream, string fileName, string? contentType, CancellationToken ct = default)
    {
        var isCsv = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            || (contentType != null && contentType.Contains("csv", StringComparison.OrdinalIgnoreCase));

        return isCsv ? ParseCsvAsync(fileStream, ct) : Task.FromResult(ParseExcel(fileStream));
    }

    private List<BankStatementRawLine> ParseExcel(Stream fileStream)
    {
        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid bank statement Excel file");
            throw new InvalidOperationException("BANK_STATEMENT_INVALID_FORMAT:Dinh dang file Excel khong hop le.");
        }

        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            workbook.Dispose();
            throw new InvalidOperationException("BANK_STATEMENT_INVALID_FORMAT:File Excel khong co sheet nao.");
        }

        var result = new List<BankStatementRawLine>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var dateCell = sheet.Cell(row, 1);
            var amountCell = sheet.Cell(row, 2);
            var referenceNo = sheet.Cell(row, 3).GetString()?.Trim();
            var description = sheet.Cell(row, 4).GetString()?.Trim();

            if (dateCell.IsEmpty() && amountCell.IsEmpty() && string.IsNullOrWhiteSpace(referenceNo) && string.IsNullOrWhiteSpace(description))
                continue; // dong trong, bo qua

            DateOnly? transactionDate = TryParseDateCell(dateCell);

            decimal amount = 0;
            if (amountCell.DataType == XLDataType.Number)
                amount = (decimal)amountCell.GetDouble();
            else
                decimal.TryParse(amountCell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);

            result.Add(new BankStatementRawLine(transactionDate, amount, referenceNo, description));
        }

        workbook.Dispose();
        return result;
    }

    private static DateOnly? TryParseDateCell(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.DateTime)
            return DateOnly.FromDateTime(cell.GetDateTime());

        var raw = cell.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (DateOnly.TryParseExact(raw, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;
        return null;
    }

    private async Task<List<BankStatementRawLine>> ParseCsvAsync(Stream fileStream, CancellationToken ct)
    {
        var result = new List<BankStatementRawLine>();
        using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);

        string? header = await reader.ReadLineAsync();
        if (header == null)
            throw new InvalidOperationException("BANK_STATEMENT_INVALID_FORMAT:File CSV rong.");

        int lineNo = 1;
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync();
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = SplitCsvLine(line);
            if (cols.Length < 2) continue; // dong khong hop le, bo qua

            DateOnly? transactionDate = null;
            if (cols.Length > 0 && !string.IsNullOrWhiteSpace(cols[0]))
            {
                var raw = cols[0].Trim();
                if (DateOnly.TryParseExact(raw, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    || DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    transactionDate = dt;
            }

            decimal amount = 0;
            if (cols.Length > 1)
                decimal.TryParse(cols[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);

            string? referenceNo = cols.Length > 2 ? cols[2].Trim() : null;
            string? description = cols.Length > 3 ? cols[3].Trim() : null;

            result.Add(new BankStatementRawLine(transactionDate, amount, referenceNo, description));
        }

        return result;
    }

    private static string[] SplitCsvLine(string line)
    {
        // Tach CSV don gian, ho tro truong bao trong dau nhay kep
        var cols = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                cols.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        cols.Add(current.ToString());
        return cols.ToArray();
    }
}
