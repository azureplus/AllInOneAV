using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Dapper;

namespace DataBaseManager
{
    public class DapperHelper
    {
        private static SqlConnection OpenConnection(string connectionStr)
        {
            var connection = new SqlConnection(connectionStr);
            connection.Open();
            return connection;
        }

        protected static T QuerySingle<T>(string connectionStr, string sql, object param = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query<T>(sql, param, null, buffered, commandTimeout, commandType).FirstOrDefault();

                return result;
            }
        }

        protected static List<T> Query<T>(string connectionStr, string sql, object param = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query<T>(sql, param, null, buffered, commandTimeout, commandType).ToList();

                return result;
            }
        }

        protected static List<dynamic> Query(string connectionStr, string sql, object param = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query(sql, param, null, buffered, commandTimeout, commandType).ToList();

                return result;
            }
        }

        protected static int Execute(string connectionStr, string sql, object param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Execute(sql, param, null, commandTimeout, commandType);
                return result;
            }
        }

        protected static void QueryMultiple(string connectionStr, string sql, Action<SqlMapper.GridReader> map, object param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.QueryMultiple(sql, param, null, commandTimeout, commandType);
                map(result);
            }
        }

        protected static List<TReturn> Query<TFirst, TSecond, TReturn>(string connectionStr, string sql, Func<TFirst, TSecond, TReturn> map, object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query(sql, map, param, null, buffered, splitOn, commandTimeout, commandType).ToList();
                return result;
            }
        }

        protected static List<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string connectionStr, string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query(sql, map, param, null, buffered, splitOn, commandTimeout, commandType).ToList();
                return result;
            }
        }

        protected static List<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string connectionStr, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query(sql, map, param, null, buffered, splitOn, commandTimeout, commandType).ToList();
                return result;
            }
        }

        protected static List<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string connectionStr, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query(sql, map, param, null, buffered, splitOn, commandTimeout, commandType).ToList();
                return result;
            }
        }

        protected static List<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string connectionStr, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query(sql, map, param, null, buffered, splitOn, commandTimeout, commandType).ToList();
                return result;
            }
        }

        protected static List<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(string connectionStr, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, object param = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var sqlConnection = OpenConnection(connectionStr))
            {
                var result = sqlConnection.Query(sql, map, param, null, buffered, splitOn, commandTimeout, commandType).ToList();
                return result;
            }
        }
    }
}
