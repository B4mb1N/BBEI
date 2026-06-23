using Microsoft.Data.Sqlite;
using System.Data;

namespace BBEIDataAccess
{
    public class SqliteDataBase : IDataBase
    {
        //public string connectionString { get; set; }
        //EntityFrameworkCore\Add-Migration AddedPrincipalTables -Context BBEIContext -OutputDir Migrations/
        public override IDbCommand CreateCommand()
        {
            return new SqliteCommand();
        }
        public override IDbCommand CreateCommand(string commandText, IDbConnection connection)
        {
            SqliteCommand command = (SqliteCommand)CreateCommand();
            command.CommandText = commandText;
            command.Connection = (SqliteConnection)connection;
            command.CommandType = CommandType.Text;
            return command;
        }
        public override IDbConnection CreateConnection()
        {
            return new SqliteConnection(connectionString);
        }
        public override IDbConnection CreateOpenConnection()
        {
            SqliteConnection connection = (SqliteConnection)CreateConnection();
            connection.Open();
            return connection;
        }
        public override IDataParameter CreateParameter(string parameterName, object parameterValue)
        {
            return new SqliteParameter() { ParameterName = parameterName, Value = parameterValue };
        }
        public override IDataParameter CreateParameter(string parameterName, object parameterValue, bool nullable)
        {
            return new SqliteParameter() { ParameterName = parameterName, Value = parameterValue, IsNullable = nullable };
        }
        public override IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction)
        {
            return new SqliteParameter() { ParameterName = parameterName, DbType = dbType, Direction = direction };
        }
        public override IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, bool nullable)
        {
            return new SqliteParameter() { ParameterName = parameterName, DbType = dbType, Direction = direction, IsNullable = nullable };
        }
        public override IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, int size)
        {
            return new SqliteParameter() { ParameterName = parameterName, DbType = dbType, Direction = direction, Size = size };
        }
        public override IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, int size, bool nullable)
        {
            return new SqliteParameter() { ParameterName = parameterName, DbType = dbType, Direction = direction, Size = size, IsNullable = nullable };
        }
        public override IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, int size, object parameterValue)
        {
            return new SqliteParameter() { ParameterName = parameterName, DbType = dbType, Direction = direction, Size = size, Value = parameterValue };
        }
        public override IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, int size, object parameterValue, bool nullable)
        {
            return new SqliteParameter() { ParameterName = parameterName, DbType = dbType, Direction = direction, Size = size, Value = parameterValue, IsNullable = nullable };
        }
        public override IDbCommand CreateStoredProcCommand(string procName, IDbConnection connection)
        {
            SqliteCommand command = (SqliteCommand)CreateCommand();
            command.CommandText = procName;
            command.Connection = (SqliteConnection)connection;
            command.CommandType = CommandType.StoredProcedure;
            return command;
        }
    }
}
