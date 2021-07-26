using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using MySqlDesktop.uow;

namespace MySqlDesktop.modules
{
    /// <summary>
    /// These are MySQL-specific methods
    /// </summary>
    public class BLL
    {
        /// <summary>
        /// List of databases
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetDatabases()
        {
            return DbConnection.Query<string>("SHOW DATABASES");
        }

        /// <summary>
        /// list of tables
        /// </summary>
        /// <param name="lboDatabaseText"></param>
        /// <returns></returns>
        public static IEnumerable<object> GetTables(string lboDatabaseText)
        {
            return DbConnection.Query<string>($"SHOW TABLES IN {lboDatabaseText}");
        }

        /// <summary>
        /// table statuses
        /// </summary>
        /// <param name="lboDatabaseText"></param>
        /// <returns></returns>
        public static IEnumerable<TableStatus> GetTableStatuses(string lboDatabaseText)
        {
            using var connection = ConnectionFactory.GetConnection();
            var sql = $"SHOW TABLE STATUS FROM `{lboDatabaseText}` WHERE ENGINE IS NOT NULL";
            return connection.Query<TableStatus>(sql).ToList();
        }

        /// <summary>
        /// table fields
        /// </summary>
        /// <param name="lboDatabaseText"></param>
        /// <param name="lboTableText"></param>
        /// <returns></returns>
        public static object GetFields(string lboDatabaseText, string lboTableText)
        {
            var sql1 = $"SHOW FIELDS IN `{lboDatabaseText}`.`{lboTableText}`";
            return DbConnection.Query<FieldInfo>(sql1);
        }
    }

    /// <summary>
    /// MySQL field info
    /// </summary>
    class FieldInfo
    {
        public string Field   { get; set; }
        public string Type    { get; set; }
        public string Null    { get; set; }
        public string Key     { get; set; }
        public string Default { get; set; }
        public string Extra   { get; set; }
    }

    /// <summary>
    /// MySQL table status
    /// </summary>
    public class TableStatus
    {
        public string Name { get; set; }

        public string Engine { get; set; }

        //public int Version { get; set; }
        //public string Row_Format { get; set; }
        public ulong Rows { get; set; }

        //public ulong Avg_row_length { get; set; }
        public ulong Data_Length { get; set; }

        //public ulong Max_data_length { get; set; }
        public ulong Index_Length { get; set; }

        //public ulong Data_free { get; set; }
        public ulong Auto_Increment { get; set; }

        public ulong Total_Length => Index_Length + Data_Length;

        //public DateTime Create_time { get; set; }
        //public DateTime Update_time { get; set; }
        //public DateTime Check_time { get; set; }
        public string Collation { get; set; }
        //public long Checksum { get; set; }
        //public string Create_options { get; set; }
        //public string Comment { get; set; }
    }
}
