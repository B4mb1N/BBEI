using System.Data;

namespace BBEIDataAccess
{
    public abstract class IDataBase
    {
        public string? connectionString;

        #region Abstract Functions

        public abstract IDbConnection CreateConnection();
        public abstract IDbCommand CreateCommand();
        public abstract IDbConnection CreateOpenConnection();
        public abstract IDbCommand CreateCommand(string commandText, IDbConnection connection);
        public abstract IDbCommand CreateStoredProcCommand(string procName, IDbConnection connection);
        public abstract IDataParameter CreateParameter(string parameterName, object parameterValue);
        public abstract IDataParameter CreateParameter(string parameterName, object parameterValue, bool nullable);
        public abstract IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction);
        public abstract IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, bool nullable);
        public abstract IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, int size);
        public abstract IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, int size, bool nullable);
        public abstract IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, int size, object parameterValue);
        public abstract IDataParameter CreateParameter(string parameterName, DbType dbType, ParameterDirection direction, int size, object parameterValue, bool nullable);

        #endregion

    }
}
