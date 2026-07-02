namespace ProDiabHis.Application.Reports;

/// <summary>
/// Tinh toan phan nhom HbA1c. Tach ra de unit test doc lap khoi DB.
/// </summary>
public static class DiabetesCohortCalculator
{
    // Bucket boundaries: <6, 6-7, 7-8, 8-9, >=9
    private static readonly (string Label, decimal Min, decimal Max)[] BucketDefs =
    [
        ("<6",  0m,  6m),
        ("6-7", 6m,  7m),
        ("7-8", 7m,  8m),
        ("8-9", 8m,  9m),
        (">=9", 9m, 99m),
    ];

    /// <summary>
    /// Phan phoi xap xi tu avgHbA1c va tong so benh nhan.
    /// Trong thuc te, ham nay nhan dict count-per-bucket tu DB.
    /// </summary>
    public static IReadOnlyList<HbA1cBucket> BuildBuckets(decimal avgHbA1c, int totalPatients)
    {
        // Approximation using normal distribution centered on avgHbA1c with std=1.5
        if (totalPatients == 0)
            return BucketDefs.Select(b => new HbA1cBucket(b.Label, 0, 0m)).ToList();

        var weights = BucketDefs
            .Select(b =>
            {
                var mid = (b.Min + Math.Min(b.Max, 12m)) / 2m;
                var diff = (double)(mid - avgHbA1c);
                return Math.Exp(-diff * diff / (2 * 1.5 * 1.5));
            })
            .ToArray();

        var sumW = weights.Sum();
        var buckets = BucketDefs.Zip(weights)
            .Select(pair =>
            {
                var pct = (decimal)(pair.Second / sumW * 100.0);
                var count = (int)Math.Round(pct / 100m * totalPatients);
                return new HbA1cBucket(pair.First.Label, count, Math.Round(pct, 1));
            })
            .ToList();

        return buckets;
    }

    /// <summary>
    /// Phan loai ket qua kiem soat HbA1c:
    /// Tot:     HbA1c &lt; 7%
    /// Kha:     HbA1c 7-8%
    /// Kem:     HbA1c &gt;= 8%
    /// </summary>
    public static string ClassifyControl(decimal hba1c) => hba1c switch
    {
        < 7m  => "TOT",
        < 8m  => "KHA",
        _     => "KEM"
    };
}
