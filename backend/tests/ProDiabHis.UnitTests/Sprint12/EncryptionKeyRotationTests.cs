using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Infrastructure.Security;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint12;

/// <summary>
/// Unit test cho EncryptionKeyStoreImpl.EncryptKeyMaterial / DecryptKeyMaterial cycle
/// va KeyRotationServiceImpl gen new version.
/// NOTE: Khong test DB/Redis — dung EncryptionKeyStoreImpl truc tiep qua reflection cho round-trip test.
/// </summary>
public class EncryptionKeyRotationTests
{
    private static EncryptionKeyStoreImpl BuildStore()
    {
        // Master key 32 bytes ngau nhien cho test
        var masterKeyBytes = RandomNumberGenerator.GetBytes(32);
        var masterKeyB64 = Convert.ToBase64String(masterKeyBytes);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = masterKeyB64
            })
            .Build();

        var dapper = Substitute.For<IDapperConnectionFactory>();
        return new EncryptionKeyStoreImpl(
            dapper,
            config,
            NullLogger<EncryptionKeyStoreImpl>.Instance,
            redis: null);
    }

    [Fact]
    public void EncryptKeyMaterial_ThenDecrypt_ShouldRoundTrip()
    {
        var store = BuildStore();

        // Gen random data key
        var originalKey = RandomNumberGenerator.GetBytes(32);

        // Encrypt
        var encrypted = store.EncryptKeyMaterial(originalKey);

        // Encrypted should be different from original
        encrypted.Should().NotEqual(originalKey);

        // Decrypt via reflection (private method test)
        var decryptMethod = typeof(EncryptionKeyStoreImpl)
            .GetMethod("DecryptKeyMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var decrypted = (byte[])decryptMethod.Invoke(store, new object[] { encrypted })!;
        decrypted.Should().Equal(originalKey);
    }

    [Fact]
    public void EncryptKeyMaterial_ShouldProduceUniqueNonceEachCall()
    {
        var store = BuildStore();
        var rawKey = RandomNumberGenerator.GetBytes(32);

        var enc1 = store.EncryptKeyMaterial(rawKey);
        var enc2 = store.EncryptKeyMaterial(rawKey);

        // Ciphertext khac nhau do nonce khac nhau (nonce random)
        enc1.Should().NotEqual(enc2);
    }

    [Fact]
    public void InvalidMasterKey_ShouldThrow_OnConstruction()
    {
        var badConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:MasterKey"] = Convert.ToBase64String(new byte[16]) // chi 16 bytes, sai
            })
            .Build();

        var dapper = Substitute.For<IDapperConnectionFactory>();
        var act = () => new EncryptionKeyStoreImpl(dapper, badConfig,
            NullLogger<EncryptionKeyStoreImpl>.Instance, null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*32 bytes*");
    }

    [Fact]
    public void DecryptInvalidData_ShouldThrow()
    {
        var store = BuildStore();
        var decryptMethod = typeof(EncryptionKeyStoreImpl)
            .GetMethod("DecryptKeyMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var act = () => decryptMethod.Invoke(store, new object[] { new byte[5] });

        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<CryptographicException>();
    }
}
