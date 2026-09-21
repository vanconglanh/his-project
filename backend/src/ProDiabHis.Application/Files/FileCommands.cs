using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Files;

// Generic file upload
public record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Category)
    : IRequest<Result<FileUploadResponse>>;

public record GetSignedUrlQuery(Guid FileId)
    : IRequest<Result<FileUploadResponse>>;

public record DeleteFileCommand(Guid FileId)
    : IRequest<Result<bool>>;

// CLS uploads
public record UploadClsCommand(
    Guid PatientId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    string DocType,
    Guid? EncounterId,
    string? Note)
    : IRequest<Result<ClsUploadResponse>>;

public record ListClsUploadsQuery(
    Guid PatientId,
    int Page,
    int PageSize,
    string? DocType)
    : IRequest<PagedResult<ClsUploadResponse>>;

public record GetClsUploadQuery(Guid PatientId, Guid Id)
    : IRequest<Result<ClsUploadResponse>>;

// BM-26 (Lark Bug-Feedback): "Không mở được Tệp tin đã upload" - signed_url tra ve tu MinIO
// tro thang toi endpoint NOI BO cua Docker network (vd "minio:9000"), trinh duyet nguoi dung
// khong resolve duoc hostname nay (KHAC voi server backend, luon o trong cung docker network
// nen goi duoc binh thuong). Doi domain sang cong khai (vd "his.diab.vn") cung khong xong: Minio
// SDK WithEndpoint() tu choi endpoint co path (vd "his.diab.vn/storage" qua proxy nginx) vi day
// la endpoint HOST, khong phai URL day du; con neu chi doi host ma khong qua path rieng thi
// route nginx hien tai (chi co /api/, /storage/ da bi strip prefix, /) khong co noi nao khop dung
// (thieu ha tang MinIO public - can domain/port rieng, ngoai pham vi 1 fix code). Fix tam thoi/
// ben vung hon KHONG phu thuoc ha tang: proxy noi dung file QUA CHINH API backend (da public san
// qua nginx location /api/, dung JWT auth co san) thay vi tra thang presigned URL cho FE.
public record GetClsUploadContentQuery(Guid PatientId, Guid Id)
    : IRequest<Result<ClsUploadFileContent>>;

public record ClsUploadFileContent(Stream Content, string ContentType, string FileName);

public record DeleteClsUploadCommand(Guid PatientId, Guid Id)
    : IRequest<Result<bool>>;

public record ListEncounterClsUploadsQuery(Guid EncounterId)
    : IRequest<List<ClsUploadResponse>>;
