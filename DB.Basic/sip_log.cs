using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using DB.Model;

namespace DB.Basic
{
    public class sip_log
    {
        public static bool Insert(sip_log_model model)
        {
            string sql = "INSERT INTO `sip_log` (`sip_auth_user`,`sip_auth_realm`,`contact`,`status`,`agent`,`host`,`addtime`) VALUES (?sip_auth_user, ?sip_auth_realm, ?contact, ?status, ?agent, ?host, ?addtime);";
            MySqlParameter[] parameters = {
     new MySqlParameter("?sip_auth_user", MySqlDbType.VarChar,50),
     new MySqlParameter("?sip_auth_realm", MySqlDbType.VarChar,50),
     new MySqlParameter("?contact", MySqlDbType.VarChar,1000),
     new MySqlParameter("?status", MySqlDbType.VarChar,1000),
     new MySqlParameter("?agent", MySqlDbType.VarChar,100),
     new MySqlParameter("?host", MySqlDbType.VarChar,100),
     new MySqlParameter("?addtime", MySqlDbType.DateTime)
                };
            parameters[0].Value = model.sip_auth_user;
            parameters[1].Value = model.sip_auth_realm;
            parameters[2].Value = model.contact;
            parameters[3].Value = model.status;
            parameters[4].Value = model.agent;
            parameters[5].Value = model.host;
            parameters[6].Value = model.addtime;
            return MySQL_Method.ExecuteNonQuery(sql, parameters) > 0;
        }

        public static bool Clear()
        {
            string m_sSQL = $@"
DELETE 
FROM
	`sip_log` 
WHERE
	addtime < DATE_SUB( NOW( ), INTERVAL 24 HOUR )
";
            return MySQL_Method.ExecuteNonQuery(m_sSQL) > 0;
        }
    }
}
