using System.Data;
using ansyl.dao;
using MySqlConnector;

namespace MySqlDesktop.uow
{
    public class ConnectionFactory
    {
        //  Set your connection String
        static string ConnString = "Data Source=localhost;User Id=root;Password=tp0506r1892;Port=40044;" +
                                   "Database=dataModel; Convert Zero Datetime=true; Use Compression=true; " +
                                   "Default Command Timeout=600; Allow User Variables=True;";

        public static IDbConnection GetConnection()
        {
            var connection = new MySqlConnection(ConnString);
            connection.Open();
            return connection;
        }
    }

    //  Unit of Work
    public class UnitOfWork : BaseUnitOfWork
    {
        public UnitOfWork() : base(ConnectionFactory.GetConnection())
        {
        }
    }

    /// <summary>
    /// OneTask - implements Transaction for a single Database task
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OneTask<T> : BaseOneTask<UnitOfWork, T> where T : DataObject
    {
    }
}