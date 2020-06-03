using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DB.Model;
using MySql.Data.MySqlClient;
using Core_v1;

namespace DB.Basic
{
	public class call_channel
	{
		public static IList<call_channel_model> GetList(string m_sAppendSQL = "")
		{
			IList<call_channel_model> list = new List<call_channel_model>();
            string sql = $"select ID,ChNo,UniqueID,ChType,ChNum,ChPassword,ShowName,SipServerIp,DomainName,SipPort,RegTime,ChName,ChVad,BoardName,BoardNo,GroupID,GroupLevel,CallType,CallRole,AutoDialCh,IsLock,Usable,Remark,IsRegister from call_channel where 1=1 {m_sAppendSQL}";
			using (var dr = MySQL_Method.ExecuteDataReader(sql))
			{
				if (dr == null || !dr.HasRows)
					return list;
				while (dr.Read())
				{
					list.Add(new call_channel_model()
					{
						ID = int.Parse(dr["ID"].ToString()),
						UniqueID = dr["UniqueID"].ToString(),
						ChNo = int.Parse(dr["ChNo"].ToString()),
						ChType = int.Parse(dr["ChType"].ToString()),
						ChNum = dr["ChNum"].ToString(),
						ChPassword = dr["ChPassword"].ToString(),
						ShowName = dr["ShowName"].ToString(),
						SipServerIp = dr["SipServerIp"].ToString(),
						DomainName = dr["DomainName"].ToString(),
						SipPort = dr["SipPort"].ToString(),
						RegTime = int.Parse(dr["RegTime"].ToString()),
						ChName = dr["ChName"].ToString(),
						ChVad = int.Parse(dr["ChVad"].ToString()),
						BoardName = dr["BoardName"].ToString(),
						BoardNo = int.Parse(dr["BoardNo"].ToString()),
						GroupID = int.Parse(dr["GroupID"].ToString()),
						GroupLevel = int.Parse(dr["GroupLevel"].ToString()),
						CallType = int.Parse(dr["CallType"].ToString()),
						CallRole = dr["CallRole"].ToString(),
						AutoDialCh = int.Parse(dr["AutoDialCh"].ToString()),
						IsLock = int.Parse(dr["IsLock"].ToString()),
						Usable = int.Parse(dr["Usable"].ToString()),
						Remark = dr["Remark"].ToString(),
                        IsRegister = int.Parse(dr["IsRegister"].ToString())
                    });
				}
			}
			return list;
		}

		public static call_channel_model GetModel(int ID)
		{
			var model = new call_channel_model();
			string sql = "select ID,UniqueID,ChNo,ChType,ChNum,ChPassword,ShowName,SipServerIp,DomainName,SipPort,RegTime,ChName,ChVad,BoardName,BoardNo,GroupID,GroupLevel,CallType,CallRole,AutoDialCh,IsLock,Usable,Remark from call_channel where ID=?ID limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?ID", MySqlDbType.Int32)
				};
			parameters[0].Value = ID;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr == null)
					return model;

				if (dr.HasRows && dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.UniqueID = dr["UniqueID"].ToString();
					model.ChNo = int.Parse(dr["ChNo"].ToString());
					model.ChType = int.Parse(dr["ChType"].ToString());
					model.ChNum = dr["ChNum"].ToString();
					model.ChPassword = dr["ChPassword"].ToString();
					model.ShowName = dr["ShowName"].ToString();
					model.SipServerIp = dr["SipServerIp"].ToString();
					model.DomainName = dr["DomainName"].ToString();
					model.SipPort = dr["SipPort"].ToString();
					model.RegTime = int.Parse(dr["RegTime"].ToString());
					model.ChName = dr["ChName"].ToString();
					model.ChVad = int.Parse(dr["ChVad"].ToString());
					model.BoardName = dr["BoardName"].ToString();
					model.BoardNo = int.Parse(dr["BoardNo"].ToString());
					model.GroupID = int.Parse(dr["GroupID"].ToString());
					model.GroupLevel = int.Parse(dr["GroupLevel"].ToString());
					model.CallType = int.Parse(dr["CallType"].ToString());
					model.CallRole = dr["CallRole"].ToString();
					model.AutoDialCh = int.Parse(dr["AutoDialCh"].ToString());
					model.IsLock = int.Parse(dr["IsLock"].ToString());
					model.Usable = int.Parse(dr["Usable"].ToString());
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}

		public static call_channel_model GetModel(string ChNo)
		{
			var model = new call_channel_model();
			string sql = "select ID,ChType,ChNum,ChPassword,ShowName,SipServerIp,DomainName,SipPort,RegTime,ChName,ChVad,BoardName,BoardNo,GroupID,GroupLevel,CallType,CallRole,AutoDialCh,IsLock,Usable,Remark from call_channel where ChNo=?ChNo limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?ChNo", MySqlDbType.Int32)
				};
			parameters[0].Value = int.Parse(ChNo);
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.ChType = int.Parse(dr["ChType"].ToString());
					model.ChNum = dr["ChNum"].ToString();
					model.ChPassword = dr["ChPassword"].ToString();
					model.ShowName = dr["ShowName"].ToString();
					model.SipServerIp = dr["SipServerIp"].ToString();
					model.DomainName = dr["DomainName"].ToString();
					model.SipPort = dr["SipPort"].ToString();
					model.RegTime = int.Parse(dr["RegTime"].ToString());
					model.ChName = dr["ChName"].ToString();
					model.ChVad = int.Parse(dr["ChVad"].ToString());
					model.BoardName = dr["BoardName"].ToString();
					model.BoardNo = int.Parse(dr["BoardNo"].ToString());
					model.GroupID = int.Parse(dr["GroupID"].ToString());
					model.GroupLevel = int.Parse(dr["GroupLevel"].ToString());
					model.CallType = int.Parse(dr["CallType"].ToString());
					model.CallRole = dr["CallRole"].ToString();
					model.AutoDialCh = int.Parse(dr["AutoDialCh"].ToString());
					model.IsLock = int.Parse(dr["IsLock"].ToString());
					model.Usable = int.Parse(dr["Usable"].ToString());
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}

		public static call_channel_model GetModelByUid(string UniqueID)
		{
			var model = new call_channel_model();
			string sql="select ID,UniqueID,ChNo,ChType,ChNum,ChPassword,ShowName,SipServerIp,DomainName,SipPort,RegTime,ChName,ChVad,BoardName,BoardNo,GroupID,GroupLevel,CallType,CallRole,AutoDialCh,IsLock,Usable,Remark from call_channel where UniqueID=?UniqueID limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?UniqueID", MySqlDbType.VarChar,36)
				};
			parameters[0].Value = UniqueID;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.UniqueID = dr["UniqueID"].ToString();
					model.ChNo = int.Parse(dr["ChNo"].ToString());
					model.ChType = int.Parse(dr["ChType"].ToString());
					model.ChNum = dr["ChNum"].ToString();
					model.ChPassword = dr["ChPassword"].ToString();
					model.ShowName = dr["ShowName"].ToString();
					model.SipServerIp = dr["SipServerIp"].ToString();
					model.DomainName = dr["DomainName"].ToString();
					model.SipPort = dr["SipPort"].ToString();
					model.RegTime = int.Parse(dr["RegTime"].ToString());
					model.ChName = dr["ChName"].ToString();
					model.ChVad = int.Parse(dr["ChVad"].ToString());
					model.BoardName = dr["BoardName"].ToString();
					model.BoardNo = int.Parse(dr["BoardNo"].ToString());
					model.GroupID = int.Parse(dr["GroupID"].ToString());
					model.GroupLevel = int.Parse(dr["GroupLevel"].ToString());
					model.CallType = int.Parse(dr["CallType"].ToString());
					model.CallRole = dr["CallRole"].ToString();
					model.AutoDialCh = int.Parse(dr["AutoDialCh"].ToString());
					model.IsLock = int.Parse(dr["IsLock"].ToString());
					model.Usable = int.Parse(dr["Usable"].ToString());
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}

        public static int m_fGetChannelType(int m_uChannelID)
        {
            try
            {
                string m_sSelectSQL = $@"
select chtype from call_channel
where id = {m_uChannelID} 
";
                return Convert.ToInt32(MySQL_Method.BindTable(m_sSelectSQL).Rows[0]["chtype"]);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][call_channel][m_fGetChannelType][Exception][{ex.Message}]");
                return -1;
            }
        }
	}
}
