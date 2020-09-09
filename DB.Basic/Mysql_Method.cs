using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using CenoCommon;
using log4net;
using Core_v1;

namespace DB.Basic {
    public class MySQL_Method {
        private static ILog _Ilog = LogManager.GetCurrentLoggers()[0];

        private static void PrepareSqlTextCommand(MySqlCommand cmd, MySqlConnection conn, string cmdText, MySqlParameter[] cmdParms) {
            if(conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            if(cmdParms != null) {
                foreach(MySqlParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }

        private static void PrepareProcedureCommand(MySqlCommand cmd, MySqlConnection conn, string cmdText, MySqlParameter[] cmdParms) {
            if(conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.StoredProcedure;
            if(cmdParms != null) {
                foreach(MySqlParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }

        /// <summary>
        /// execute sql with params and return the number of affected rows
        /// </summary>
        /// <param name="cmdText">cmdText</param>
        /// <param name="cmdParms">the number of affected rows</param>
        /// <returns>the number of affected rows</returns>
        public static int ExecuteNonQuery(string cmdText, MySqlParameter[] cmdParms = null, string m_sConnStr = null) {
            MySqlCommand cmd = new MySqlCommand();
            if (string.IsNullOrWhiteSpace(m_sConnStr))
                m_sConnStr = MySQLDBConnectionString.ConnectionString;
            MySqlConnection _SqlConnection = new MySqlConnection(m_sConnStr);
            try {
                PrepareSqlTextCommand(cmd, _SqlConnection, cmdText, cmdParms);
                int val = cmd.ExecuteNonQuery();
                return val;
            } catch(Exception ex) {
                _Ilog.Fatal($"sql:{cmdText},catch an exception:" + ex.Message, ex);
                return 0;
            } finally {
                cmd.Parameters.Clear();
                _SqlConnection.Close();
                _SqlConnection.Dispose();
                cmd.Dispose();
            }
        }

        /// <summary>
        /// execute sql and return datatable
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="cmdParms"></param>
        /// <returns></returns>
        public static DataSet GetDataSetAll(string cmdText, MySqlParameter[] cmdParms = null, string m_sConnStr = null)
        {
            DataSet ds = new DataSet();
            if (string.IsNullOrWhiteSpace(m_sConnStr)) m_sConnStr = MySQLDBConnectionString.ConnectionString;
            MySqlConnection _SqlConnection = new MySqlConnection(m_sConnStr);
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataAdapter adpt = new MySqlDataAdapter();
            try
            {
                PrepareSqlTextCommand(cmd, _SqlConnection, cmdText, cmdParms);
                adpt.SelectCommand = cmd;
                adpt.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                _Ilog.Fatal($"sql:{cmdText},catch an exception:" + ex.Message, ex);
            }
            finally
            {
                cmd.Parameters.Clear();
                _SqlConnection.Close();
                _SqlConnection.Dispose();
                adpt.Dispose();
                cmd.Dispose();
            }
            return null;
        }

        /// <summary>
        /// execute sql and return datatable
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="cmdParms"></param>
        /// <returns></returns>
        public static DataTable BindTable(string cmdText, MySqlParameter[] cmdParms = null, string m_sConnStr = null)
        {
            DataTable dt = null;
            if (string.IsNullOrWhiteSpace(m_sConnStr)) m_sConnStr = MySQLDBConnectionString.ConnectionString;
            MySqlConnection _SqlConnection = new MySqlConnection(m_sConnStr);
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataAdapter adpt = new MySqlDataAdapter();
            try
            {
                PrepareSqlTextCommand(cmd, _SqlConnection, cmdText, cmdParms);
                adpt.SelectCommand = cmd;
                DataSet ds = new DataSet();
                adpt.Fill(ds);
                return ds.Tables[0];
            }
            catch (Exception ex)
            {
                _Ilog.Fatal($"sql:{cmdText},catch an exception:" + ex.Message, ex);
            }
            finally
            {
                cmd.Parameters.Clear();
                _SqlConnection.Close();
                _SqlConnection.Dispose();
                adpt.Dispose();
                cmd.Dispose();
            }
            return dt;
        }

        /// <summary>
        /// execute sql with params and return DataReader
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="cmdParms"></param>
        /// <returns></returns>
        public static MySqlDataReader ExecuteDataReader(string cmdText, MySqlParameter[] cmdParms = null) {
            MySqlCommand cmd = new MySqlCommand();
            MySqlConnection _SqlConnection = new MySqlConnection(MySQLDBConnectionString.ConnectionString);
            try
            {
                PrepareSqlTextCommand(cmd, _SqlConnection, cmdText, cmdParms);
                MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                return reader;
            }
            catch (Exception ex)
            {
                cmd.Parameters.Clear();
                _SqlConnection.Close();
                _SqlConnection.Dispose();
                cmd.Dispose();
                _Ilog.Fatal($"sql:{cmdText},catch an exception:" + ex.Message, ex);
            }
            return null;
        }

        /// <summary>
        /// execute sql with params and return the frist row and frist column 
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="cmdParms"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string cmdText, MySqlParameter[] cmdParms = null) {
            MySqlCommand cmd = new MySqlCommand();
            MySqlConnection _SqlConnection = new MySqlConnection(MySQLDBConnectionString.ConnectionString);
            object ExcuteRst = new object();
            try {
                PrepareSqlTextCommand(cmd, _SqlConnection, cmdText, cmdParms);
                using(MySqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection)) {
                    cmd.Parameters.Clear();
                    if(reader.HasRows && reader.Read())
                        ExcuteRst = reader[0];
                }
            } catch(Exception ex) {
                _Ilog.Fatal($"sql:{cmdText},catch an exception:" + ex.Message, ex);
            } finally {
                cmd.Parameters.Clear();
                _SqlConnection.Close();
                _SqlConnection.Dispose();
                cmd.Dispose();
            }
            return ExcuteRst;
        }

        /// <summary>
        ///  Execute Procedure with params and return dataset
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="cmdParms"></param>
        /// <returns></returns>
        public static DataSet ExecuteDataSetByProcedure(string m_sConnStr, string cmdText, MySqlParameter[] cmdParms = null)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataAdapter adpt = new MySqlDataAdapter();
            if (string.IsNullOrWhiteSpace(m_sConnStr)) m_sConnStr = MySQLDBConnectionString.ConnectionString;
            MySqlConnection _SqlConnection = new MySqlConnection(m_sConnStr);
            DataSet ds = new DataSet();
            try
            {
                PrepareProcedureCommand(cmd, _SqlConnection, cmdText, cmdParms);
                adpt.SelectCommand = cmd;
                adpt.Fill(ds);
            }
            catch (Exception ex)
            {
                _Ilog.Fatal($"sql:{cmdText},catch an exception:" + ex.Message, ex);
            }
            finally
            {
                cmd.Parameters.Clear();
                _SqlConnection.Close();
                _SqlConnection.Dispose();
                adpt.Dispose();
                cmd.Dispose();
            }
            return ds;
        }

        public static DataSet ExecuteDataSetByProcedure(string cmdText, MySqlParameter[] cmdParms, string m_sConnStr = null)
        {
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataAdapter adpt = new MySqlDataAdapter();
            if (string.IsNullOrWhiteSpace(m_sConnStr)) m_sConnStr = MySQLDBConnectionString.ConnectionString;
            MySqlConnection _SqlConnection = new MySqlConnection(m_sConnStr);
            DataSet ds = new DataSet();
            try
            {
                PrepareProcedureCommand(cmd, _SqlConnection, cmdText, cmdParms);
                adpt.SelectCommand = cmd;
                adpt.Fill(ds);
            }
            catch (Exception ex)
            {
                _Ilog.Fatal($"sql:{cmdText},catch an exception:" + ex.Message, ex);
            }
            finally
            {
                cmd.Parameters.Clear();
                _SqlConnection.Close();
                _SqlConnection.Dispose();
                adpt.Dispose();
                cmd.Dispose();
            }
            return ds;
        }
    }
}
