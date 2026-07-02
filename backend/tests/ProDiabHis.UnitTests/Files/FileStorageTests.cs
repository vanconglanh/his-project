using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Files;
using Xunit;

namespace ProDiabHis.UnitTests.Files;

public class FileStorageTests
{
    [Fact]
    public void FileBuckets_Constants_AreDefinedCorrectly()
    {
        FileBuckets.Avatars.Should().Be("avatars");
        FileBuckets.ClsUploads.Should().Be("cls-uploads");
        FileBuckets.FilesGeneric.Should().Be("files-generic");
    }

    [Fact]
    public void UploadFileCommand_StoresProperties()
    {
        using var stream = new MemoryStream();
        var cmd = new UploadFileCommand(stream, "test.pdf", "application/pdf", 1024, "CONSENT");
        cmd.FileName.Should().Be("test.pdf");
        cmd.ContentType.Should().Be("application/pdf");
        cmd.SizeBytes.Should().Be(1024);
        cmd.Category.Should().Be("CONSENT");
    }

    [Fact]
    public void UploadClsCommand_StoresProperties()
    {
        var patientId = Guid.NewGuid();
        using var stream = new MemoryStream();
        var cmd = new UploadClsCommand(patientId, stream, "xquang.jpg", "image/jpeg", 512000, "X-quang phổi", null, "Ghi chú test");
        cmd.PatientId.Should().Be(patientId);
        cmd.DocType.Should().Be("X-quang phổi");
        cmd.Note.Should().Be("Ghi chú test");
    }

    [Fact]
    public async Task UploadFileCommandHandler_FileTooLarge_ReturnsFailure()
    {
        // Arrange
        var dbFactory = Substitute.For<IDapperConnectionFactory>();
        var tenant = Substitute.For<ITenantProvider>();
        var currentUser = Substitute.For<ICurrentUser>();
        var storage = Substitute.For<IFileStorage>();

        tenant.TenantId.Returns(1);
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new UploadFileCommandHandler(dbFactory, tenant, currentUser, storage);

        using var stream = new MemoryStream(new byte[100]);
        var cmd = new UploadFileCommand(stream, "big.pdf", "application/pdf",
            21L * 1024 * 1024, // 21MB > 20MB limit
            null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_UPLOAD_FAILED");
    }

    [Fact]
    public async Task UploadClsCommandHandler_InvalidFormat_ReturnsFailure()
    {
        var dbFactory = Substitute.For<IDapperConnectionFactory>();
        var tenant = Substitute.For<ITenantProvider>();
        var currentUser = Substitute.For<ICurrentUser>();
        var storage = Substitute.For<IFileStorage>();

        tenant.TenantId.Returns(1);
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new UploadClsCommandHandler(dbFactory, tenant, currentUser, storage);

        using var stream = new MemoryStream(new byte[100]);
        var cmd = new UploadClsCommand(
            Guid.NewGuid(), stream, "doc.docx", "application/msword",
            1024, "Bao cao", null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CLS_UPLOAD_INVALID_FORMAT");
    }

    [Fact]
    public async Task UploadClsCommandHandler_FileTooLarge_ReturnsFailure()
    {
        var dbFactory = Substitute.For<IDapperConnectionFactory>();
        var tenant = Substitute.For<ITenantProvider>();
        var currentUser = Substitute.For<ICurrentUser>();
        var storage = Substitute.For<IFileStorage>();

        tenant.TenantId.Returns(1);
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new UploadClsCommandHandler(dbFactory, tenant, currentUser, storage);

        using var stream = new MemoryStream(new byte[100]);
        var cmd = new UploadClsCommand(
            Guid.NewGuid(), stream, "big.jpg", "image/jpeg",
            11L * 1024 * 1024, // 11MB > 10MB
            "X-quang", null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CLS_UPLOAD_TOO_LARGE");
    }
}
