using ProDiabHis.Application.Common;

namespace ProDiabHis.UnitTests;

using System.Data;
using System.Data.Common;

/// <summary>
/// Fake ADO.NET DbConnection tra ve rong cho MOI query (khong co du lieu that).
/// Dung de test cac handler goi Dapper qua IDapperConnectionFactory ma khong can DB that.
/// LUU Y: PHAI la DbConnection (khong phai mock IDbConnection thuan) vi Dapper chi dung duoc
/// nhanh async that (ExecuteReaderAsync/ExecuteNonQueryAsync) khi connection la DbConnection;
/// neu truyen mock IDbConnection (vd NSubstitute), Dapper roi ve nhanh dong bo tren mot object
/// khong duoc setup -> nem loi khi await.
/// </summary>
#pragma warning disable CS8764, CS8765 // override cua ADO.NET base co nullable annotation khong dong nhat giua .NET version
public sealed class FakeEmptyDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;
    public override string ConnectionString { get; set; } = "Data Source=fake;";
    public override string Database => "fake";
    public override string DataSource => "fake";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName) { }
    public override void Close() => _state = ConnectionState.Closed;
    public override void Open() => _state = ConnectionState.Open;
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => throw new NotSupportedException("FakeEmptyDbConnection khong ho tro transaction");
    protected override DbCommand CreateDbCommand() => new FakeEmptyDbCommand { Connection = this };
}

internal sealed class FakeEmptyDbCommand : DbCommand
{
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection { get; } = new FakeEmptyParameterCollection();
    protected override DbTransaction? DbTransaction { get; set; }
    public override bool DesignTimeVisible { get; set; }

    public override void Cancel() { }
    public override int ExecuteNonQuery() => 0;
    public override object? ExecuteScalar() => null;
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => new FakeEmptyParameter();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new FakeEmptyDataReader();
}

internal sealed class FakeEmptyParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    public override string ParameterName { get; set; } = string.Empty;
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }
    public override void ResetDbType() { }
}

internal sealed class FakeEmptyParameterCollection : DbParameterCollection
{
    private readonly List<object> _items = new();
    public override int Count => _items.Count;
    public override object SyncRoot { get; } = new();
    public override int Add(object value) { _items.Add(value); return _items.Count - 1; }
    public override void AddRange(Array values) { foreach (var v in values) _items.Add(v!); }
    public override void Clear() => _items.Clear();
    public override bool Contains(string value) => false;
    public override bool Contains(object value) => _items.Contains(value);
    public override void CopyTo(Array array, int index) => _items.CopyTo((object[])array, index);
    public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
    protected override DbParameter GetParameter(string parameterName) => (DbParameter)_items[0];
    protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];
    public override int IndexOf(string parameterName) => -1;
    public override int IndexOf(object value) => _items.IndexOf(value);
    public override void Insert(int index, object value) => _items.Insert(index, value);
    public override void Remove(object value) => _items.Remove(value);
    public override void RemoveAt(string parameterName) { }
    public override void RemoveAt(int index) => _items.RemoveAt(index);
    protected override void SetParameter(string parameterName, DbParameter value) { }
    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
}

internal sealed class FakeEmptyDataReader : DbDataReader
{
    public override int FieldCount => 0;
    public override int RecordsAffected => 0;
    public override bool HasRows => false;
    public override bool IsClosed => false;
    public override int Depth => 0;
    public override object this[int ordinal] => throw new NotSupportedException();
    public override object this[string name] => throw new NotSupportedException();

    public override bool Read() => false;
    public override bool NextResult() => false;
    public override System.Collections.IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

    public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
    public override byte GetByte(int ordinal) => throw new NotSupportedException();
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => throw new NotSupportedException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override string GetDataTypeName(int ordinal) => "";
    public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
    public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
    public override double GetDouble(int ordinal) => throw new NotSupportedException();
    public override Type GetFieldType(int ordinal) => typeof(object);
    public override float GetFloat(int ordinal) => throw new NotSupportedException();
    public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
    public override short GetInt16(int ordinal) => throw new NotSupportedException();
    public override int GetInt32(int ordinal) => throw new NotSupportedException();
    public override long GetInt64(int ordinal) => throw new NotSupportedException();
    public override string GetName(int ordinal) => "";
    public override int GetOrdinal(string name) => -1;
    public override string GetString(int ordinal) => throw new NotSupportedException();
    public override object GetValue(int ordinal) => throw new NotSupportedException();
    public override int GetValues(object[] values) => 0;
    public override bool IsDBNull(int ordinal) => true;
}

/// <summary>IDapperConnectionFactory tra ve <see cref="FakeEmptyDbConnection"/> — dung cho unit test
/// khong can du lieu tra ve tu Dapper (query luon rong, ExecuteAsync luon 0 dong).</summary>
public sealed class FakeEmptyDapperConnectionFactory : IDapperConnectionFactory
{
    public IDbConnection CreateConnection() => new FakeEmptyDbConnection();
}
#pragma warning restore CS8764, CS8765


/// <summary>
/// Fake IPiiProtector cho unit test.
/// Khoa HMAC o day la khoa TEST-ONLY (khong phai secret production) — khoa that lay tu
/// bien moi truong Encryption:BlindIndexKey.
/// </summary>
public class FakePiiProtector : IPiiProtector
{
    public const string Marker = "enc:v1:";
    private static readonly byte[] TestKey = System.Text.Encoding.UTF8.GetBytes("unit-test-blind-index-key-32bytes!!");
    private readonly IEncryptionService _enc = new FakeEncryptionService();

    public string? Protect(string? plaintext)
        => string.IsNullOrEmpty(plaintext) || IsProtected(plaintext) ? plaintext : Marker + _enc.Encrypt(plaintext);

    public string? Unprotect(string? stored)
        => string.IsNullOrEmpty(stored) || !IsProtected(stored) ? stored : _enc.Decrypt(stored[Marker.Length..]);

    public bool IsProtected(string? stored)
        => !string.IsNullOrEmpty(stored) && stored.StartsWith(Marker, StringComparison.Ordinal);

    public string? BlindIndex(string? plaintext, PiiField field)
    {
        var normalized = PiiNormalizer.Normalize(plaintext, field);
        if (string.IsNullOrEmpty(normalized)) return null;
        var hash = System.Security.Cryptography.HMACSHA256.HashData(
            TestKey, System.Text.Encoding.UTF8.GetBytes($"{field}:{normalized}"));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>Fake IEncryptionService cho unit test (encode/decode Base64 don gian)</summary>
public class FakeEncryptionService : IEncryptionService
{
    public string Encrypt(string plaintext) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));
    public string Decrypt(string ciphertext) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
}
