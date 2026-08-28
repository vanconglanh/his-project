using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Auth;

/// <summary>Scoped service luu branch context cho request hien tai</summary>
public class BranchProvider : IBranchProvider
{
    private int _branchId;
    private bool _ignoreBranchFilter;
    private IReadOnlyList<int> _allowedBranchIds = Array.Empty<int>();

    public int BranchId => _branchId;
    public bool IgnoreBranchFilter => _ignoreBranchFilter;
    public IReadOnlyList<int> AllowedBranchIds => _allowedBranchIds;

    public void SetContext(int branchId, bool ignoreFilter, IReadOnlyList<int> allowedBranchIds)
    {
        _branchId = branchId;
        _ignoreBranchFilter = ignoreFilter;
        _allowedBranchIds = allowedBranchIds;
    }
}
