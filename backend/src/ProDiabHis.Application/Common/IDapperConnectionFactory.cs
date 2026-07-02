using System.Data;

namespace ProDiabHis.Application.Common;

/// <summary>Factory tao IDbConnection cho Dapper read queries</summary>
public interface IDapperConnectionFactory
{
    IDbConnection CreateConnection();
}
