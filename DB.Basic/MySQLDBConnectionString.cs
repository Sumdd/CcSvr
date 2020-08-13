using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Configuration;
using Cmn_v1;

namespace DB.Basic {
    public class MySQLDBConnectionString {
        private static string _ConnectionString;
        public static string ConnectionString {
            get {
                if(string.IsNullOrEmpty(_ConnectionString)) {
                    if (!string.IsNullOrEmpty(DB_Server) && !string.IsNullOrEmpty(DB_Name) && !string.IsNullOrEmpty(DB_Uid) && !string.IsNullOrEmpty(DB_Pwd))
                        _ConnectionString = "server=" + Cmn.m_fRemoveSpace(DB_Server) + ";database=" + Cmn.m_fRemoveSpace(DB_Name) + ";user id=" + Cmn.m_fRemoveSpace(DB_Uid) + ";password=" + Core_v1.m_cSafe.DecryptString(DB_Pwd) + ";Charset=utf8";
                    else
                        _ConnectionString = "server=192.168.0.220;database=cmcp10;user id=root;password=123;Charset=utf8";
                }
                return _ConnectionString;
            }
        }

        public static string m_fConnStr(Model_v1.dial_area m_pDialArea)
        {
            try
            {
                if (m_pDialArea == null)
                    throw new ArgumentNullException("m_pDialArea");

                if (m_pDialArea.aip == null)
                    throw new ArgumentNullException("aip");

                if (m_pDialArea.adb == null)
                    throw new ArgumentNullException("adb");

                if (m_pDialArea.auid == null)
                    throw new ArgumentNullException("auid");

                if (m_pDialArea.apwd == null)
                    throw new ArgumentNullException("apwd");

                return $"server={Cmn.m_fRemoveSpace(m_pDialArea.aip)};database={Cmn.m_fRemoveSpace(m_pDialArea.adb)};user id={Cmn.m_fRemoveSpace(m_pDialArea.auid)};password={Core_v1.m_cSafe.DecryptString(m_pDialArea.apwd)};Charset=utf8";
            }
            catch (Exception ex)
            {
                Core_v1.Log.Instance.Error($"[DB.Basic][MySQLDBConnectionString][m_fConnStr][Exception][{ex.Message}]");
                return null;
            }
        }

        public static string m_fConnStr(string m_sIP)
        {
            try
            {
                if (m_sIP == null)
                    throw new ArgumentNullException("m_pDialArea");

                return $"server={Cmn.m_fRemoveSpace(m_sIP)};database=cmcp10;user id=root;password=123;Charset=utf8";
            }
            catch (Exception ex)
            {
                Core_v1.Log.Instance.Error($"[DB.Basic][MySQLDBConnectionString][m_fConnStr][Exception][{ex.Message}]");
                return null;
            }
        }

        public static string DB_Server {
            get {
                return Properties.Settings.Default.DB_Server;
            }
            set {
                Properties.Settings.Default.DB_Server = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string DB_Name {
            get {
                return Properties.Settings.Default.DB_Name;
            }
            set {
                Properties.Settings.Default.DB_Name = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string DB_Uid {
            get {
                return Properties.Settings.Default.DB_Uid;
            }
            set {
                Properties.Settings.Default.DB_Uid = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string DB_Pwd {
            get {
                return Properties.Settings.Default.DB_Pwd;
            }
            set {
                Properties.Settings.Default.DB_Pwd = value;
                Properties.Settings.Default.Save();
            }
        }

        private static string _fs_connectionString;
        public static string fs_connectionString {
            get {
                if(string.IsNullOrEmpty(_fs_connectionString)) {
                    if (string.IsNullOrEmpty(fs_db_server) || string.IsNullOrEmpty(DB_Name) || string.IsNullOrEmpty(DB_Uid) || string.IsNullOrEmpty(DB_Pwd))
                        _fs_connectionString = "server=192.168.0.220;database=cmcp10;user id=root;password=123;Charset=utf8";
                    else
                        _fs_connectionString = "server=" + DB_Server + ";database=" + fs_db_name + ";user id=" + fs_db_uid + ";password=" + Core_v1.m_cSafe.DecryptString(fs_db_pwd) + ";Charset=utf8";
                }
                return _ConnectionString;
            }
        }

        public static string fs_db_server {
            get {
                return Properties.Settings.Default.fs_db_server;
            }
            set {
                Properties.Settings.Default.fs_db_server = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string fs_db_name {
            get {
                return Properties.Settings.Default.fs_db_name;
            }
            set {
                Properties.Settings.Default.fs_db_name = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string fs_db_uid {
            get {
                return Properties.Settings.Default.fs_db_uid;
            }
            set {
                Properties.Settings.Default.fs_db_uid = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string fs_db_pwd {
            get {
                return Properties.Settings.Default.fs_db_pwd;
            }
            set {
                Properties.Settings.Default.fs_db_pwd = value;
                Properties.Settings.Default.Save();
            }
        }
    }
}
