namespace ProDiabHis.Application.Auth;

/// <summary>Bam va xac thuc mat khau</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
