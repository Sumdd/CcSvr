using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using DB.Model;
using System.Data;

namespace DB.Basic
{
    public class call_agent_basic
    {
        public static IList<call_agent_model> GetList(string m_sSQL = "")
        {
            IList<call_agent_model> list = new List<call_agent_model>();
            string sql = $@"
SELECT
	`call_agent`.`ID`,
	UniqueID,
	AgentName,
	LoginName,
	LoginPassWord,
	LoginState,
	AgentNumber,
	AgentPassword,
	LastLoginIp,
	TeamID,
	StateID,
	RoleID,
	ChannelID,
	ClientParamID,
	Usable,
	LinkUser,
	LU_LoginName,
	LU_Password,
	`call_agent`.`Remark`,
	`call_clientparam`.`isinlimit_2`,
	`call_clientparam`.`inlimit_2number`,
	`call_clientparam`.`inlimit_2starttime`,
	`call_clientparam`.`inlimit_2endtime`,
	`call_clientparam`.`inlimit_2whatday`,
	`call_clientparam`.`limitthedial` 
FROM
	call_agent
	LEFT JOIN `call_clientparam` ON `call_clientparam`.`ID` = `call_agent`.`ClientParamID` 
WHERE
	1 = 1 
    {m_sSQL} 
	LIMIT {Model_v1.m_cModel.m_uUa}";
            using (var dr = MySQL_Method.ExecuteDataReader(sql))
            {
                while (dr != null && dr.HasRows && dr.Read())
                {
                    list.Add(new call_agent_model()
                    {
                        ID = int.Parse(dr["ID"].ToString()),
                        UniqueID = dr["UniqueID"].ToString(),
                        AgentName = dr["AgentName"].ToString(),
                        LoginName = dr["LoginName"].ToString(),
                        LoginPassWord = dr["LoginPassWord"].ToString(),
                        LoginState = int.Parse(dr["LoginState"].ToString()),
                        AgentNumber = dr["AgentNumber"].ToString(),
                        AgentPassword = dr["AgentPassword"].ToString(),
                        LastLoginIp = dr["LastLoginIp"].ToString(),
                        TeamID = int.Parse(dr["TeamID"].ToString()),
                        StateID = int.Parse(dr["StateID"].ToString()),
                        RoleID = int.Parse(dr["RoleID"].ToString()),
                        ChannelID = int.Parse(dr["ChannelID"].ToString()),
                        ClientParamID = int.Parse(dr["ClientParamID"].ToString()),
                        Usable = int.Parse(dr["Usable"].ToString()),
                        LinkUser = int.Parse(dr["LinkUser"].ToString()),
                        LU_LoginName = dr["LU_LoginName"].ToString(),
                        LU_Password = dr["LU_Password"].ToString(),
                        Remark = dr["Remark"].ToString(),
                        ///呼叫内转配置
                        isinlimit_2 = dr["isinlimit_2"]?.ToString() == "1" ? true : false,
                        inlimit_2number = dr["inlimit_2number"]?.ToString(),
                        inlimit_2starttime = dr["inlimit_2starttime"]?.ToString(),
                        inlimit_2endtime = dr["inlimit_2endtime"]?.ToString(),
                        inlimit_2whatday = int.Parse(dr["inlimit_2whatday"]?.ToString()),
                        limitthedial = int.Parse(dr["limitthedial"]?.ToString())
                    });
                }
            }
            return list;
        }

        public static int UpdateAgentLoginState(string State, string Ipaddress, string UserID)
        {
            StringBuilder Sqlstr = new StringBuilder();
            Sqlstr.Append("update Call_Agent set ");
            Sqlstr.Append("LoginState=@LoginState,");
            Sqlstr.Append("LastLoginIp=@LastLoginIp");
            Sqlstr.Append(" where ID=@ID");

            MySqlParameter[] parameters = {
                new MySqlParameter("@LoginState",State),
                new MySqlParameter("@LastLoginIp",Ipaddress),
                new MySqlParameter("@ID",UserID)};

            return MySQL_Method.ExecuteNonQuery(Sqlstr.ToString(), parameters);
        }

        public static DataTable m_fGetOperatePower()
        {
            try
            {

            }
            catch (Exception ex)
            {
                Core_v1.Log.Instance.Error($"[DB.Basic][call_agent_basic][m_fGetOperatePower][Exception][{ex.Message}]");
            }
            return null;
        }
    }
}
