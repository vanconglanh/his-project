using MySqlConnector;
using ProDiabHis.Application.Common;
using System.Data;

namespace ProDiabHis.Infrastructure.Dapper;

/// <summary>Factory tao IDbConnection cho Dapper read queries</summary>
public class DapperConnectionFactory : IDapperConnectionFactory
{
    private readonly string _connectionString;

    public DapperConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
