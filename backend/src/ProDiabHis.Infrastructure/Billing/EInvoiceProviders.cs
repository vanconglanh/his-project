using ProDiabHis.Application.Billing;

namespace ProDiabHis.Infrastructure.Billing;

/// <summary>MISA eInvoice - mock dev: gen random CQT code 13 ky tu</summary>
public class MisaEInvoiceProvider : IEInvoiceProvider
{
    public string ProviderName => "MISA";

    public Task<EInvoiceIssueResult> IssueAsync(EInvoiceIssueRequest request, CancellationToken ct = default)
    {
        var cqtCode = GenerateCqtCode();
        var invoiceNo = $"0001{DateTime.Now:yyyyMMddHHmmss}";
        return Task.FromResult(new EInvoiceIssueResult(
            cqtCode, invoiceNo, "M23TCA", null, null));
    }

    public Task<EInvoiceCancelResult> CancelAsync(string invoiceNo, string reason, CancellationToken ct = default)
        => Task.FromResult(new EInvoiceCancelResult(true, null));

    public Task<string> GetXmlAsync(string invoiceNo, CancellationToken ct = default)
    {
        var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<HDon Ma=""{invoiceNo}"" xmlns=""http://laphoadon.gdt.gov.vn/2014/09/phieu"">
  <TTChung><SHDon>1</SHDon><KHMSHDon>1</KHMSHDon><KHHDon>M23TCA</KHHDon></TTChung>
  <NDHDon><NBan><Ten>PHONG KHAM PRO DIAB</Ten></NBan></NDHDon>
</HDon>";
        return Task.FromResult(xml);
    }

    private static string GenerateCqtCode()
    {
        // 13 ky tu: 6 chu so ngau nhien + 7 chu hoa
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rng = new Random();
        return new string(Enumerable.Range(0, 13).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}

/// <summary>VNPT eInvoice - mock dev</summary>
public class VnptEInvoiceProvider : IEInvoiceProvider
{
    public string ProviderName => "VNPT";

    public Task<EInvoiceIssueResult> IssueAsync(EInvoiceIssueRequest request, CancellationToken ct = default)
    {
        var cqtCode = GenerateCqtCode();
        var invoiceNo = $"V{DateTime.Now:yyyyMMddHHmmss}";
        return Task.FromResult(new EInvoiceIssueResult(cqtCode, invoiceNo, "V23TCB", null, null));
    }

    public Task<EInvoiceCancelResult> CancelAsync(string invoiceNo, string reason, CancellationToken ct = default)
        => Task.FromResult(new EInvoiceCancelResult(true, null));

    public Task<string> GetXmlAsync(string invoiceNo, CancellationToken ct = default)
        => Task.FromResult($"<invoice no=\"{invoiceNo}\" provider=\"VNPT\"/>");

    private static string GenerateCqtCode()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rng = new Random();
        return new string(Enumerable.Range(0, 13).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}

/// <summary>EFY eInvoice - mock dev</summary>
public class EfyEInvoiceProvider : IEInvoiceProvider
{
    public string ProviderName => "EFY";

    public Task<EInvoiceIssueResult> IssueAsync(EInvoiceIssueRequest request, CancellationToken ct = default)
    {
        var cqtCode = GenerateCqtCode();
        var invoiceNo = $"E{DateTime.Now:yyyyMMddHHmmss}";
        return Task.FromResult(new EInvoiceIssueResult(cqtCode, invoiceNo, "E23TCC", null, null));
    }

    public Task<EInvoiceCancelResult> CancelAsync(string invoiceNo, string reason, CancellationToken ct = default)
        => Task.FromResult(new EInvoiceCancelResult(true, null));

    public Task<string> GetXmlAsync(string invoiceNo, CancellationToken ct = default)
        => Task.FromResult($"<invoice no=\"{invoiceNo}\" provider=\"EFY\"/>");

    private static string GenerateCqtCode()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rng = new Random();
        return new string(Enumerable.Range(0, 13).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}
