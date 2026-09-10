using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.CLS;

// BM-12: CRUD bo chi dinh mau CLS, scope theo doctor_id (nguoi tao) - khong chia se
// giua cac bac si trong cung tenant (PO xac nhan "rieng tung bac si").

public record CreateClsOrderTemplateCommand(CreateClsOrderTemplateRequest Request)
    : IRequest<Result<ClsOrderTemplateResponse>>;

public record ListClsOrderTemplatesQuery() : IRequest<Result<ClsOrderTemplateListResponse>>;

public record DeleteClsOrderTemplateCommand(Guid Id) : IRequest<Result>;
