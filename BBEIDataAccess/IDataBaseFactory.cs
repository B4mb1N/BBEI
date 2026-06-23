namespace BBEIDataAccess
{
    public interface IDataBaseFactory
    {
        IDataBase CreateDatabase(string _dataType, string _connection);
        //private static string dataType;
        //private static string connectionString;
        //public DataBaseFactory(string _dataType, string _connectionString)
        //{
        //    dataType = _dataType;
        //    connectionString = _connectionString;
        //}

        //public static IDataBase CreateDataBase()
        //{
        //    if (string.IsNullOrEmpty(dataType))
        //        throw new Exception("Database name not defined in config file");
        //    try
        //    {
        //        Type database = Type.GetType(dataType);
        //        ConstructorInfo constructorInfo = database.GetConstructor(new Type[] { });
        //        IDataBase databaseObj = (IDataBase)constructorInfo.Invoke(null);
        //        //databaseObj.connectionString = connectionString;
        //        return databaseObj;
        //    }
        //    catch (Exception excep)
        //    {
        //        throw new Exception("Error instantiating database " + dataType + ". " + excep.Message);
        //    }
        //}
    }
}
