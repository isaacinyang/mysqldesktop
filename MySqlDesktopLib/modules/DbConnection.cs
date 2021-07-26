using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using MySqlConnector;
using MySqlDesktop.uow;

namespace MySqlDesktop.modules
{
    public class DbConnection
    {
        //public static string ConnectionString { get; set; }

        /// <summary>
        /// connection object
        /// </summary>
        /// <returns></returns>
        public static IDbConnection GetConnection() => ConnectionFactory.GetConnection();
        //{
        //    var connection = new MySqlConnection(ConnectionString);
        //    connection.Open();
        //    return connection;
        //}

        //public static int Execute(string sql, object data = null)
        //{
        //    using var connection = GetConnection();
        //    return connection.Execute(sql, data ?? new { });
        //}

        //public static T ExecuteScalar<T>(string sql, object data = null)
        //{
        //    using var connection = GetConnection();
        //    return connection.ExecuteScalar<T>(sql, data ?? new { });
        //}

        /// <summary>
        /// Query for a List of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IList<T> Query<T>(string sql, object data = null)
        {
            using var connection = GetConnection();
            return connection.Query<T>(sql, data ?? new { }).ToList();
        }
    }
}