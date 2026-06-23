using SQLitePCL;
using System.Reflection;

namespace BBEIDataAccess
{
    public class DataBaseFactory : IDataBaseFactory
    {
        public IDataBase CreateDatabase(string _dataType, string _connection)
        {
            if (string.IsNullOrEmpty(_dataType))
                throw new Exception("Database name not defined in config file");
            try
            {
                Type? database = Type.GetType(_dataType);
                ConstructorInfo? constructorInfo = database?.GetConstructor(new Type[] { });
                IDataBase? databaseObj = (IDataBase)constructorInfo.Invoke(null);
                databaseObj.connectionString = _connection;
                return databaseObj;
            }
            catch (Exception excep)
            {
                throw new Exception("Error instantiating database " + _dataType + ". " + excep.Message);
            }
        }
    }
}
