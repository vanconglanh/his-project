using global::Dapper;
using MySqlConnector;
using ProDiabHis.Application.Common;
using System.Data;

namespace ProDiabHis.Infrastructure.Dapper;

/// <summary>Factory tao IDbConnection cho Dapper read queries</summary>
public class DapperConnectionFactory : IDapperConnectionFactory
{
    private readonly string _connectionString;

    static DapperConnectionFactory()
    {
        // MySqlConnector khong tu bind DateOnly qua Dapper (vd expected_delivery, mfg_date)
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
    }

    public DapperConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}

internal class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
        => parameter.Value = value.ToDateTime(TimeOnly.MinValue);

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        string s => DateOnly.Parse(s),
        _ => throw new DataException($"Khong the convert {value.GetType()} sang DateOnly")
    };
}

internal class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly? value)
        => parameter.Value = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;

    public override DateOnly? Parse(object value) => value switch
    {
        null or DBNull => null,
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        string s => DateOnly.Parse(s),
        _ => throw new DataException($"Khong the convert {value.GetType()} sang DateOnly?")
    };
}
