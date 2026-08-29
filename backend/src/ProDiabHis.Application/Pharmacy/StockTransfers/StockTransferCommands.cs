using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Pharmacy.StockTransfers;

// ─── Commands ───────────────────────────────────────────────────────────────
public record CreateStockTransferCommand(CreateStockTransferRequest Request) : IRequest<Result<StockTransferResponse>>;
public record SubmitStockTransferCommand(string Id) : IRequest<Result<StockTransferResponse>>;
public record ApproveStockTransferCommand(string Id, ApproveStockTransferRequest Request) : IRequest<Result<StockTransferResponse>>;
public record RejectStockTransferCommand(string Id, RejectStockTransferRequest Request) : IRequest<Result<StockTransferResponse>>;
public record ShipStockTransferCommand(string Id) : IRequest<Result<StockTransferResponse>>;
public record ReceiveStockTransferCommand(string Id, ReceiveStockTransferRequest Request) : IRequest<Result<StockTransferResponse>>;
public record PartialReceiveStockTransferCommand(string Id, ReceiveStockTransferRequest Request) : IRequest<Result<StockTransferResponse>>;
public record CloseStockTransferCommand(string Id) : IRequest<Result<StockTransferResponse>>;
public record CancelStockTransferCommand(string Id) : IRequest<Result<StockTransferResponse>>;

// ─── Queries ────────────────────────────────────────────────────────────────
public record ListStockTransfersQuery(string? Status, int? BranchId, int Page, int PageSize) : IRequest<Result<PagedResult<StockTransferResponse>>>;
public record GetStockTransferQuery(string Id) : IRequest<Result<StockTransferResponse>>;
