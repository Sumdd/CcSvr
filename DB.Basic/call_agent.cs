using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using DB.Model;

namespace DB.Basic {
    public class call_agent_basic {
        public static IList<call_agent_model> GetList(int top = 1000) {
            IList<call_agent_model> list = new List<call_agent_model>();
            string sql = "select ID,UniqueID,AgentName,LoginName,LoginPassWord,LoginState,AgentNumber,AgentPassword,LastLoginIp,TeamID,StateID,RoleID,ChannelID,ClientParamID,Usable,LinkUser,LU_LoginName,LU_Password,Remark from call_agent limit ?top";
            MySqlParameter[] parameters = {
     new MySqlParameter("?top", MySqlDbType.Int32)
                };
            parameters[0].Value = top;
            using(var dr = MySQL_Method.ExecuteDataReader(sql, parameters)) {
                while(dr != null && dr.HasRows && dr.Read()) {
                    list.Add(new call_agent_model() {
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
                        Remark = dr["Remark"].ToString()
                    });
                }
            }
            return list;
        }

        public static int UpdateAgentLoginState(string State, string Ipaddress, string UserID) {
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
    }
}
